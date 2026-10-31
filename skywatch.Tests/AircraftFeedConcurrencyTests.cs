using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class AircraftFeedConcurrencyTests
{
    private sealed class SlowSource : IAircraftSource
    {
        public int Calls;
        public string Name => "slow";

        public async Task<IReadOnlyList<AircraftState>> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(100, ct);
            return [TestAircraft.Make()];
        }

        public Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct) => Task.FromResult<AircraftState?>(null);
    }

    [Fact]
    public async Task Concurrent_requests_for_one_area_share_a_single_upstream_call()
    {
        var source = new SlowSource();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var locks = new KeyedLock();

        // One feed per request, like the scoped registration, sharing the cache and the lock.
        var snapshots = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            new AircraftFeed([source], cache, locks, new AircraftAreaIndex(), Options.Create(new AircraftOptions()), NullLogger<AircraftFeed>.Instance)
                .GetAroundAsync(51, 0, 50, default)));

        Assert.Equal(1, source.Calls);
        Assert.All(snapshots, s => Assert.Single(s.Aircraft));
    }

    private sealed class FailingSource : IAircraftSource
    {
        public int Calls;
        public string Name => "failing";

        public async Task<IReadOnlyList<AircraftState>> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(50, ct);
            throw new HttpRequestException("down");
        }

        public Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct) => Task.FromResult<AircraftState?>(null);
    }

    [Fact]
    public async Task Requests_queued_behind_a_total_outage_do_not_call_the_sources_again()
    {
        var source = new FailingSource();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var locks = new KeyedLock();

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            var feed = new AircraftFeed([source], cache, locks, new AircraftAreaIndex(), Options.Create(new AircraftOptions()), NullLogger<AircraftFeed>.Instance);
            return await Record.ExceptionAsync(() => feed.GetAroundAsync(51, 0, 50, default));
        }));

        Assert.Equal(1, source.Calls);
        Assert.All(results, e => Assert.IsType<AircraftFeedUnavailableException>(e));
    }

    [Fact]
    public async Task The_sources_are_tried_again_once_the_failure_has_expired()
    {
        var source = new FailingSource();
        var feed = new AircraftFeed([source], new MemoryCache(new MemoryCacheOptions()), new KeyedLock(), new AircraftAreaIndex(),
            Options.Create(new AircraftOptions { FailureTtl = TimeSpan.FromMilliseconds(50) }), NullLogger<AircraftFeed>.Instance);

        await Assert.ThrowsAsync<AircraftFeedUnavailableException>(() => feed.GetAroundAsync(51, 0, 50, default));
        await Task.Delay(120);
        await Assert.ThrowsAsync<AircraftFeedUnavailableException>(() => feed.GetAroundAsync(51, 0, 50, default));

        Assert.Equal(2, source.Calls);
    }
}
