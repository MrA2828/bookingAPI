namespace SkyWatch.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AirportEndpointTests(SkyWatchFactory api)
{
    private async Task<JsonElement> GetJson(string url)
    {
        var response = await api.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string[] Ids(JsonElement items) => items.EnumerateArray().Select(i => i.GetProperty("id").GetInt32().ToString()).ToArray();

    [Fact]
    public async Task Search_by_iata_code_puts_the_exact_match_first()
    {
        var items = await GetJson("/airports?search=lhr");
        Assert.Equal("LHR", items[0].GetProperty("iataCode").GetString());
    }

    [Fact]
    public async Task Search_matches_names_and_cities_case_insensitively()
    {
        var byCity = await GetJson("/airports?search=luton");
        Assert.Equal("EGGW", byCity[0].GetProperty("ident").GetString());
    }

    [Fact]
    public async Task Search_treats_like_wildcards_literally()
    {
        var items = await GetJson("/airports?search=%25");
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public async Task Inactive_airports_are_never_returned()
    {
        Assert.Equal(0, (await GetJson("/airports?search=zombie")).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync("/airports/ZMB")).StatusCode);
    }

    [Fact]
    public async Task Country_filter_only_returns_that_country()
    {
        var items = await GetJson("/airports?country=us");
        var airport = Assert.Single(items.EnumerateArray());
        Assert.Equal("US", airport.GetProperty("countryCode").GetString());
    }

    [Fact]
    public async Task Bigger_airports_sort_before_smaller_ones()
    {
        var items = await GetJson("/airports?country=GB&pageSize=10");
        var types = items.EnumerateArray().Select(i => i.GetProperty("type").GetString()).ToList();
        Assert.Equal(["large_airport", "large_airport", "medium_airport", "medium_airport", "small_airport"], types);
    }

    [Fact]
    public async Task Pages_do_not_overlap_and_run_out()
    {
        var first = await GetJson("/airports?country=XX&pageSize=50&pageNumber=1");
        var second = await GetJson("/airports?country=XX&pageSize=50&pageNumber=2");
        var third = await GetJson("/airports?country=XX&pageSize=50&pageNumber=3");
        var fourth = await GetJson("/airports?country=XX&pageSize=50&pageNumber=4");

        Assert.Equal(50, first.GetArrayLength());
        Assert.Equal(50, second.GetArrayLength());
        Assert.Equal(20, third.GetArrayLength());
        Assert.Equal(0, fourth.GetArrayLength());
        Assert.Empty(Ids(first).Intersect(Ids(second)));
        Assert.Empty(Ids(second).Intersect(Ids(third)));
    }

    [Fact]
    public async Task Page_size_is_capped_at_100_and_page_number_floors_at_1()
    {
        var items = await GetJson("/airports?country=XX&pageSize=1000&pageNumber=0");
        Assert.Equal(100, items.GetArrayLength());
    }

    [Fact]
    public async Task Gets_one_airport_by_iata_icao_or_ident_and_404s_for_unknown()
    {
        foreach (var code in new[] { "LHR", "egll" })
            Assert.Equal("London Heathrow Airport", (await GetJson($"/airports/{code}")).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync("/airports/NOPE")).StatusCode);
    }

    [Fact]
    public async Task Nearby_returns_the_nearest_scheduled_airports_first_and_pages()
    {
        var first = await GetJson("/airports/nearby?lat=51.47&lon=-0.46&radiusKm=100&pageSize=2&pageNumber=1");
        var second = await GetJson("/airports/nearby?lat=51.47&lon=-0.46&radiusKm=100&pageSize=2&pageNumber=2");

        Assert.Equal(["EGLL", "EGLC"], first.EnumerateArray().Select(i => i.GetProperty("airport").GetProperty("ident").GetString()));
        Assert.Equal(["EGKK", "EGGW"], second.EnumerateArray().Select(i => i.GetProperty("airport").GetProperty("ident").GetString()));
    }

    [Fact]
    public async Task Nearby_hides_unscheduled_airfields_unless_asked()
    {
        var scheduled = await GetJson("/airports/nearby?lat=51.47&lon=-0.46&radiusKm=100&pageSize=100");
        var all = await GetJson("/airports/nearby?lat=51.47&lon=-0.46&radiusKm=100&pageSize=100&scheduledOnly=false");

        Assert.DoesNotContain(scheduled.EnumerateArray(), i => i.GetProperty("airport").GetProperty("ident").GetString() == "EGXX");
        Assert.Contains(all.EnumerateArray(), i => i.GetProperty("airport").GetProperty("ident").GetString() == "EGXX");
    }

    [Fact]
    public async Task Nearby_respects_the_radius()
    {
        // From the search point: Heathrow ~1 km, City ~36 km, Gatwick ~41 km.
        var small = await GetJson("/airports/nearby?lat=51.47&lon=-0.46&radiusKm=30&pageSize=100");
        var larger = await GetJson("/airports/nearby?lat=51.47&lon=-0.46&radiusKm=38&pageSize=100");

        Assert.Equal(["EGLL"], small.EnumerateArray().Select(i => i.GetProperty("airport").GetProperty("ident").GetString()));
        Assert.Equal(["EGLL", "EGLC"], larger.EnumerateArray().Select(i => i.GetProperty("airport").GetProperty("ident").GetString()));
    }

    [Theory]
    [InlineData("/airports/nearby?radiusKm=50")]
    [InlineData("/airports/nearby?lat=51&radiusKm=50")]
    [InlineData("/airports/nearby?lat=91&lon=0")]
    [InlineData("/airports/nearby?lat=51&lon=0&radiusKm=501")]
    public async Task Nearby_rejects_missing_or_out_of_range_input(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync(url)).StatusCode);
}
