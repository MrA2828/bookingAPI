using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class AircraftFeedTests
{
    private static readonly IReadOnlyList<AircraftState> OnePlane = [TestAircraft.Make()];

    private static AircraftFeed Feed(AircraftOptions options, params IAircraftSource[] sources) =>
        new(sources, new MemoryCache(new MemoryCacheOptions()), new KeyedLock(), Options.Create(options), NullLogger<AircraftFeed>.Instance);

    private static IReadOnlyList<AircraftState> Boom() => throw new HttpRequestException("down");

    [Fact]
    public async Task Uses_the_first_source_when_it_works()
    {
        var primary = new FakeAircraftSource("primary", () => OnePlane);
        var fallback = new FakeAircraftSource("fallback", () => OnePlane);

        var snapshot = await Feed(new AircraftOptions(), primary, fallback).GetAroundAsync(51, 0, 50, default);

        Assert.Equal("primary", snapshot.Source);
        Assert.Equal(0, fallback.Calls);
    }

    [Fact]
    public async Task Falls_back_to_the_next_source_when_the_first_fails()
    {
        var snapshot = await Feed(new AircraftOptions(),
                new FakeAircraftSource("primary", Boom), new FakeAircraftSource("fallback", () => OnePlane))
            .GetAroundAsync(51, 0, 50, default);

        Assert.Equal("fallback", snapshot.Source);
        Assert.False(snapshot.Stale);
        Assert.Single(snapshot.Aircraft);
    }

    [Fact]
    public async Task Serves_repeat_requests_for_the_same_area_from_cache()
    {
        var source = new FakeAircraftSource("primary", () => OnePlane);
        var feed = Feed(new AircraftOptions(), source);

        await feed.GetAroundAsync(51.02, 0.01, 50, default);
        await feed.GetAroundAsync(51.03, -0.02, 50, default); // snaps to the same 0.1 degree cell

        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task Returns_the_last_snapshot_flagged_stale_when_every_source_fails()
    {
        var failing = false;
        var source = new FakeAircraftSource("primary", () => failing ? Boom() : OnePlane);
        var feed = Feed(new AircraftOptions { CacheTtl = TimeSpan.Zero }, source);

        await feed.GetAroundAsync(51, 0, 50, default);
        failing = true;
        var snapshot = await feed.GetAroundAsync(51, 0, 50, default);

        Assert.True(snapshot.Stale);
        Assert.Single(snapshot.Aircraft);
    }

    [Fact]
    public async Task Throws_when_every_source_fails_and_nothing_is_cached()
    {
        var feed = Feed(new AircraftOptions(), new FakeAircraftSource("a", Boom), new FakeAircraftSource("b", Boom));
        await Assert.ThrowsAsync<AircraftFeedUnavailableException>(() => feed.GetAroundAsync(51, 0, 50, default));
    }

    [Fact]
    public async Task Cancellation_is_not_swallowed_as_a_source_failure()
    {
        using var cts = new CancellationTokenSource();
        var source = new FakeAircraftSource("a", () => { cts.Cancel(); throw new OperationCanceledException(cts.Token); });
        var feed = Feed(new AircraftOptions(), source, new FakeAircraftSource("b", () => OnePlane));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => feed.GetAroundAsync(51, 0, 50, cts.Token));
    }

    [Fact]
    public async Task Finds_one_aircraft_by_hex()
    {
        var feed = Feed(new AircraftOptions(), new FakeAircraftSource("a", () => OnePlane));
        Assert.NotNull(await feed.GetByHexAsync("abc123", default));
        Assert.Null(await feed.GetByHexAsync("ffffff", default));
    }
}
