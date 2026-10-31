namespace SkyWatch.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AirlineEndpointTests(SkyWatchFactory api)
{
    private async Task<JsonElement> GetJson(string url)
    {
        var response = await api.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task An_exact_code_match_comes_before_name_matches()
    {
        // "BA" is British Airways' IATA code and also appears inside "Baltic Test Air".
        var items = await GetJson("/airlines?search=ba");
        Assert.Equal("British Airways", items[0].GetProperty("name").GetString());
        Assert.Contains(items.EnumerateArray(), i => i.GetProperty("name").GetString() == "Baltic Test Air");
    }

    [Fact]
    public async Task Inactive_airlines_are_never_returned() =>
        Assert.Equal(0, (await GetJson("/airlines?search=defunct")).GetArrayLength());

    [Fact]
    public async Task Pages_do_not_overlap()
    {
        var first = await GetJson("/airlines?search=carrier&pageSize=10&pageNumber=1");
        var second = await GetJson("/airlines?search=carrier&pageSize=10&pageNumber=2");
        var third = await GetJson("/airlines?search=carrier&pageSize=10&pageNumber=3");

        Assert.Equal([10, 10, 5], new[] { first, second, third }.Select(p => p.GetArrayLength()));
        var all = new[] { first, second, third }.SelectMany(p => p.EnumerateArray()).Select(i => i.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(25, all.Distinct().Count());
    }

    [Fact]
    public async Task Page_size_is_capped_at_100()
    {
        var items = await GetJson("/airlines?pageSize=1000");
        Assert.Equal(28, items.GetArrayLength()); // every active seeded airline: 25 carriers + 3 named
    }
}
