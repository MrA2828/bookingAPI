using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Queries;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class AircraftInViewHandlerTests
{
    // The view is western Europe; every circle that touches it reports the same overlapping traffic.
    private static readonly AircraftState[] Sky =
    [
        TestAircraft.Make("000001", "BAW1", "G-AAAA", 51.5, -0.1, 35000),
        TestAircraft.Make("000002", "UAL2", "N111AA", 48.8, 2.3, 12000),
        TestAircraft.Make("000003", "BAW3", "G-BBBB", 52.3, 4.8, 39000),
        TestAircraft.Make("000004", "GABCD", "G-ABCD", 50.0, 8.0, 3000),                  // private
        TestAircraft.Make("000005", "BAW5", "G-CCCC", 51.0, 0.0, null, onGround: true),   // parked
        TestAircraft.Make("000006", "BAW6", "G-DDDD", 10.0, 10.0, 30000),                 // outside the view
    ];

    private static AircraftInViewHandler Handler(IAircraftFeed feed, AircraftOptions? options = null) =>
        new(feed, new FakeAirlineCodes("BAW", "UAL"), Options.Create(options ?? new AircraftOptions()));

    private static AircraftInViewQuery Query(AircraftKind[]? kinds = null, int? minAlt = null, bool onGround = false, int limit = 1500) =>
        new(new AircraftInViewRequestDto
        {
            South = 45, North = 58, West = -10, East = 12, Kinds = kinds, MinAltitudeFt = minAlt, IncludeOnGround = onGround, Limit = limit
        });

    private static FakeTiledAircraftFeed Everywhere() => new((_, _) => Sky);

    [Fact]
    public async Task Merges_the_overlapping_circles_without_duplicates_and_sorts_highest_first()
    {
        var feed = Everywhere();
        var result = await Handler(feed).Handle(Query(), default);

        Assert.True(feed.Calls > 1); // the view needed several circles
        Assert.Equal(["000003", "000001", "000002"], result.Items.Select(i => i.Hex));
        Assert.Equal(3, result.TotalCount);
        Assert.True(result.Complete);
    }

    [Fact]
    public async Task Drops_aircraft_outside_the_rectangle()
    {
        var result = await Handler(Everywhere()).Handle(Query(), default);
        Assert.DoesNotContain("000006", result.Items.Select(i => i.Hex));
    }

    [Fact]
    public async Task Applies_the_same_filters_as_the_radius_search()
    {
        var military = await Handler(Everywhere()).Handle(Query(kinds: [AircraftKind.Private]), default);
        Assert.Equal(["000004"], military.Items.Select(i => i.Hex));

        var high = await Handler(Everywhere()).Handle(Query(minAlt: 20000), default);
        Assert.Equal(["000003", "000001"], high.Items.Select(i => i.Hex));

        var parked = await Handler(Everywhere()).Handle(Query(onGround: true), default);
        Assert.Contains("000005", parked.Items.Select(i => i.Hex));
    }

    [Fact]
    public async Task Limit_keeps_the_highest_aircraft_and_total_count_still_reports_every_match()
    {
        var result = await Handler(Everywhere()).Handle(Query(limit: 2), default);

        Assert.Equal(["000003", "000001"], result.Items.Select(i => i.Hex));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task A_circle_that_fails_leaves_the_view_incomplete_but_still_answers()
    {
        // Only circles centred west of 0 degrees answer.
        var feed = new FakeTiledAircraftFeed((_, lon) => lon < 0 ? Sky : null);
        var result = await Handler(feed).Handle(Query(), default);

        Assert.False(result.Complete);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task Fails_with_the_feed_exception_when_no_circle_can_be_read()
    {
        var feed = new FakeTiledAircraftFeed((_, _) => null);
        await Assert.ThrowsAsync<AircraftFeedUnavailableException>(() => Handler(feed).Handle(Query(), default));
    }

    [Fact]
    public async Task Asks_the_feed_for_circles_as_old_as_the_wide_view_setting_allows()
    {
        var feed = Everywhere();
        await Handler(feed, new AircraftOptions { WideViewMaxAge = TimeSpan.FromSeconds(45) }).Handle(Query(), default);

        Assert.Equal(TimeSpan.FromSeconds(45), feed.LastMaxAge);
    }

    [Fact]
    public async Task Is_stale_when_any_circle_is_stale()
    {
        var result = await Handler(new FakeAircraftFeed(Sky, stale: true)).Handle(Query(), default);
        Assert.True(result.Stale);
    }
}
