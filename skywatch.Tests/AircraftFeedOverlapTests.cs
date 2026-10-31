using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

// A request whose circle lies inside a fresh, larger cached circle is answered from it.
public class AircraftFeedOverlapTests
{
    private static readonly AircraftState NearTheSmallCentre = TestAircraft.Make("aaaaaa", latitude: 51.3, longitude: 0.2);
    private static readonly AircraftState SixtyMilesNorth = TestAircraft.Make("bbbbbb", latitude: 52.0, longitude: 0.0);

    private static (AircraftFeed Feed, FakeAircraftSource Source) Feed(AircraftOptions? options = null)
    {
        var source = new FakeAircraftSource("primary", () => [NearTheSmallCentre, SixtyMilesNorth]);
        var feed = new AircraftFeed([source], new MemoryCache(new MemoryCacheOptions()), new KeyedLock(), new AircraftAreaIndex(),
            Options.Create(options ?? new AircraftOptions()), NullLogger<AircraftFeed>.Instance);
        return (feed, source);
    }

    [Fact]
    public async Task A_circle_inside_a_cached_one_is_served_from_it_without_calling_a_source()
    {
        var (feed, source) = Feed();
        await feed.GetAroundAsync(51.0, 0.0, 100, default);

        var snapshot = await feed.GetAroundAsync(51.3, 0.2, 20, default);

        Assert.Equal(1, source.Calls);
        Assert.False(snapshot.Stale);
        Assert.Equal(["aaaaaa"], snapshot.Aircraft.Select(a => a.Hex)); // the aircraft 60 miles away is cut off
    }

    [Fact]
    public async Task A_circle_that_sticks_out_of_the_cached_one_is_fetched()
    {
        var (feed, source) = Feed();
        await feed.GetAroundAsync(51.0, 0.0, 100, default);

        await feed.GetAroundAsync(51.0, 0.0, 150, default);   // bigger
        await feed.GetAroundAsync(53.5, 0.0, 20, default);    // elsewhere

        Assert.Equal(3, source.Calls);
    }

    [Fact]
    public async Task An_expired_cached_circle_is_not_used()
    {
        var (feed, source) = Feed(new AircraftOptions { CacheTtl = TimeSpan.Zero });
        await feed.GetAroundAsync(51.0, 0.0, 100, default);

        await feed.GetAroundAsync(51.3, 0.2, 20, default);

        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public void The_index_keeps_only_the_most_recent_areas_and_lists_the_smallest_cover_first()
    {
        var index = new AircraftAreaIndex();
        index.Add("big", 51, 0, 200);
        index.Add("small", 51, 0, 60);
        index.Add("far", 10, 10, 200);

        Assert.Equal(["small", "big"], index.FindCovering(51, 0, 50));

        for (var i = 0; i < 250; i++) index.Add($"filler{i}", 0, 0, 10);
        Assert.Empty(index.FindCovering(51, 0, 50)); // the old areas were pushed out
    }
}
