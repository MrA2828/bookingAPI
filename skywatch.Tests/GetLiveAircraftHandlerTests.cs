using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Queries;

namespace SkyWatch.Tests;

public class GetLiveAircraftHandlerTests
{
    // Everything sits within a few km of 51.5, -0.1.
    private static readonly AircraftState[] Sky =
    [
        TestAircraft.Make("000001", "BAW1", "G-AAAA", 51.50, -0.10, 35000),                         // commercial, nearest
        TestAircraft.Make("000002", "UAL2", "N111AA", 51.52, -0.10, 12000),                         // commercial
        TestAircraft.Make("000003", "GABCD", "G-ABCD", 51.54, -0.10, 3000),                         // private
        TestAircraft.Make("000004", "RRR9", "ZZ100", 51.56, -0.10, 25000, military: true),          // military
        TestAircraft.Make("000005", "QQQ7", "G-QQQQ", 51.58, -0.10, 20000),                         // unknown
        TestAircraft.Make("000006", "BAW3", "G-BBBB", 51.60, -0.10, null, onGround: true),          // commercial, parked
        TestAircraft.Make("000007", "BAW4", "G-CCCC", 60.00, -0.10, 30000),                         // far away
    ];

    private static GetLiveAircraftHandler Handler(bool stale = false) =>
        new(new FakeAircraftFeed(Sky, stale), new FakeAirlineCodes("BAW", "UAL"));

    private static GetLiveAircraftQuery Query(IReadOnlyList<AircraftKind>? kinds = null, int? minAlt = null, int? maxAlt = null,
        bool includeOnGround = false, double radiusKm = 100, int pageNumber = 1, int pageSize = 20) =>
        new(51.5, -0.1, radiusKm, kinds, minAlt, maxAlt, includeOnGround, pageNumber, pageSize);

    private static string[] Hexes(SkyWatch.Features.Aircraft.Dtos.LiveAircraftPageDto page) => page.Items.Select(i => i.Hex).ToArray();

    [Fact]
    public async Task By_default_excludes_military_private_and_grounded_aircraft_and_sorts_nearest_first()
    {
        var page = await Handler().Handle(Query(), default);
        Assert.Equal(["000001", "000002", "000005"], Hexes(page));
    }

    [Fact]
    public async Task Drops_aircraft_outside_the_radius()
    {
        var page = await Handler().Handle(Query(radiusKm: 100), default);
        Assert.DoesNotContain("000007", Hexes(page));
    }

    [Fact]
    public async Task Kinds_filter_selects_exactly_what_was_asked_for()
    {
        var page = await Handler().Handle(Query(kinds: [AircraftKind.Military, AircraftKind.Private]), default);
        Assert.Equal(["000003", "000004"], Hexes(page));
    }

    [Fact]
    public async Task Altitude_filters_apply_and_exclude_aircraft_with_no_altitude()
    {
        var page = await Handler().Handle(Query(minAlt: 15000, maxAlt: 30000, includeOnGround: true), default);
        Assert.Equal(["000005"], Hexes(page)); // 12000 too low, 35000 too high, ground has none
    }

    [Fact]
    public async Task Include_on_ground_adds_parked_aircraft()
    {
        var page = await Handler().Handle(Query(includeOnGround: true), default);
        Assert.Contains("000006", Hexes(page));
    }

    [Fact]
    public async Task Pages_through_results_and_reports_the_total()
    {
        var first = await Handler().Handle(Query(pageSize: 2, pageNumber: 1), default);
        var second = await Handler().Handle(Query(pageSize: 2, pageNumber: 2), default);

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(["000001", "000002"], Hexes(first));
        Assert.Equal(["000005"], Hexes(second));
    }

    [Fact]
    public async Task Clamps_page_number_and_page_size()
    {
        var page = await Handler().Handle(Query(pageNumber: 0, pageSize: 1000), default);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(100, page.PageSize);
    }

    [Fact]
    public async Task Passes_the_stale_flag_through()
    {
        var page = await Handler(stale: true).Handle(Query(), default);
        Assert.True(page.Stale);
    }
}
