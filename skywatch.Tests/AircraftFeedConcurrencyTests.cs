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
            new AircraftFeed([source], cache, locks, Options.Create(new AircraftOptions()), NullLogger<AircraftFeed>.Instance)
                .GetAroundAsync(51, 0, 50, default)));

        Assert.Equal(1, source.Calls);
        Assert.All(snapshots, s => Assert.Single(s.Aircraft));
    }
}
