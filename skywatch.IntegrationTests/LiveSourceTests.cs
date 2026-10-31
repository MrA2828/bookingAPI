using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.IntegrationTests;

// These call the real public services, so they need internet and can fail when a service is down or
// rate-limiting. Skip them with: dotnet test --filter "Category!=Network"
[Trait("Category", "Network")]
[Collection(ApiCollection.Name)]
public class LiveSourceTests(SkyWatchFactory api)
{
    private static HttpClient Http(string baseUrl) => new()
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { UserAgent = { new("SkyWatch-Tests", "1.0") } }
    };

    // London has airliners overhead at any hour, so an empty answer means the source or the parser is broken.
    [Fact]
    public async Task Adsb_lol_returns_parsed_aircraft_around_London()
    {
        var aircraft = await new AdsbLolSource(Http("https://api.adsb.lol/")).GetAroundAsync(51.47, -0.45, 100, default);
        Assert.NotEmpty(aircraft);
        Assert.All(aircraft, a => Assert.Matches("^[0-9a-f]{6}$", a.Hex));
        Assert.All(aircraft, a => Assert.InRange(a.Latitude, 49, 54));
    }

    [Fact]
    public async Task Adsb_fi_returns_parsed_aircraft_around_London()
    {
        var aircraft = await new AdsbFiSource(Http("https://opendata.adsb.fi/")).GetAroundAsync(51.47, -0.45, 100, default);
        Assert.NotEmpty(aircraft);
        Assert.All(aircraft, a => Assert.Matches("^[0-9a-f]{6}$", a.Hex));
    }

    [Fact]
    public async Task Both_sources_find_a_known_aircraft_by_hex()
    {
        var sample = (await new AdsbLolSource(Http("https://api.adsb.lol/")).GetAroundAsync(51.47, -0.45, 100, default)).First();

        var fromFi = await new AdsbFiSource(Http("https://opendata.adsb.fi/")).GetByHexAsync(sample.Hex, default);
        var fromLol = await new AdsbLolSource(Http("https://api.adsb.lol/")).GetByHexAsync(sample.Hex, default);

        // An aircraft can drop off the network between calls, so only check agreement when both still see it.
        if (fromFi is not null && fromLol is not null) Assert.Equal(fromLol.Hex, fromFi.Hex);
        Assert.True(fromFi is not null || fromLol is not null);
    }

    [Fact]
    public async Task Adsbdb_returns_a_route_with_coordinates_or_cleanly_returns_none()
    {
        var provider = new AdsbdbFlightRouteProvider(Http("https://api.adsbdb.com/"), new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new AircraftOptions()), NullLogger<AdsbdbFlightRouteProvider>.Instance);

        var route = await provider.GetRouteAsync("BAW117", default);

        if (route is null) return; // flight not scheduled today; the point is that it did not throw
        Assert.Equal("BAW117", route.Callsign);
        Assert.NotNull(route.Destination);
        Assert.InRange(route.Destination!.Latitude, -90, 90);
    }

    [Fact]
    public async Task Adsbdb_returns_none_for_a_nonsense_callsign()
    {
        var provider = new AdsbdbFlightRouteProvider(Http("https://api.adsbdb.com/"), new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new AircraftOptions()), NullLogger<AdsbdbFlightRouteProvider>.Instance);

        Assert.Null(await provider.GetRouteAsync("ZZZZ0000", default));
    }

    [Fact]
    public async Task The_aircraft_endpoint_serves_live_traffic_end_to_end()
    {
        var response = await api.Client.GetAsync("/aircraft?lat=51.47&lon=-0.45&radiusKm=150&pageSize=50&kinds=commercial&kinds=private&kinds=unknown&kinds=military&includeOnGround=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(page.GetProperty("source").GetString(), new[] { "adsb.lol", "adsb.fi" });
        Assert.False(page.GetProperty("stale").GetBoolean());
        Assert.True(page.GetProperty("totalCount").GetInt32() > 0);

        var distances = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("distanceKm").GetDouble()).ToList();
        Assert.Equal(distances.OrderBy(d => d), distances);
        Assert.All(distances, d => Assert.True(d <= 150));
    }

    [Fact]
    public async Task The_detail_endpoint_returns_the_aircraft_that_the_list_returned()
    {
        var list = await (await api.Client.GetAsync("/aircraft?lat=51.47&lon=-0.45&radiusKm=150&pageSize=1&kinds=commercial&kinds=unknown"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var hex = list.GetProperty("items")[0].GetProperty("hex").GetString();

        var response = await api.Client.GetAsync($"/aircraft/{hex}");

        if (response.StatusCode == HttpStatusCode.NotFound) return; // it left coverage in the last second
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(hex, detail.GetProperty("aircraft").GetProperty("hex").GetString());
    }

    [Fact]
    public async Task An_unknown_hex_is_a_404()
    {
        var response = await api.Client.GetAsync("/aircraft/000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
