using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

// A caller that can live with older positions (a wide map view) passes maxAge; everyone else gets CacheTtl.
public class AircraftFeedMaxAgeTests
{
    private static (AircraftFeed Feed, FakeAircraftSource Source) Feed(TimeSpan cacheTtl)
    {
        var source = new FakeAircraftSource("primary", () => [TestAircraft.Make()]);
        var feed = new AircraftFeed([source], new MemoryCache(new MemoryCacheOptions()), new KeyedLock(), new AircraftAreaIndex(),
            Options.Create(new AircraftOptions { CacheTtl = cacheTtl }), NullLogger<AircraftFeed>.Instance);
        return (feed, source);
    }

    [Fact]
    public async Task A_copy_older_than_cache_ttl_is_refetched_by_default()
    {
        var (feed, source) = Feed(cacheTtl: TimeSpan.Zero);

        await feed.GetAroundAsync(51, 0, 100, default);
        await feed.GetAroundAsync(51, 0, 100, default);

        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public async Task A_longer_max_age_accepts_the_same_copy()
    {
        var (feed, source) = Feed(cacheTtl: TimeSpan.Zero);

        await feed.GetAroundAsync(51, 0, 100, default);
        await feed.GetAroundAsync(51, 0, 100, default, maxAge: TimeSpan.FromSeconds(30));

        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task A_longer_max_age_also_applies_to_a_covering_circle()
    {
        var (feed, source) = Feed(cacheTtl: TimeSpan.Zero);

        await feed.GetAroundAsync(51, 0, 200, default);
        await feed.GetAroundAsync(51, 0, 50, default, maxAge: TimeSpan.FromSeconds(30));

        Assert.Equal(1, source.Calls);
    }
}
