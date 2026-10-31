namespace SkyWatch.IntegrationTests;

// Input validation only: these never reach the live ADS-B sources, so they are fast and offline.
[Collection(ApiCollection.Name)]
public class AircraftValidationTests(SkyWatchFactory api)
{
    [Theory]
    [InlineData("/aircraft?radiusKm=100&includeOnGround=false&pageNumber=1&pageSize=20")] // Swagger's default request
    [InlineData("/aircraft?lat=51")]
    [InlineData("/aircraft?lon=0")]
    [InlineData("/aircraft?lat=91&lon=0")]
    [InlineData("/aircraft?lat=51&lon=181")]
    [InlineData("/aircraft?lat=51&lon=0&radiusKm=441")]
    [InlineData("/aircraft?lat=51&lon=0&radiusKm=0")]
    [InlineData("/aircraft?lat=51&lon=0&kinds=spaceship")]
    public async Task List_rejects_missing_or_invalid_input(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync(url)).StatusCode);

    [Theory]
    [InlineData("/aircraft/in-view")]
    [InlineData("/aircraft/in-view?south=50&north=51&west=0")]
    [InlineData("/aircraft/in-view?south=51&north=50&west=0&east=1")]
    [InlineData("/aircraft/in-view?south=50&north=51&west=1&east=0")]
    [InlineData("/aircraft/in-view?south=-91&north=51&west=0&east=1")]
    [InlineData("/aircraft/in-view?south=50&north=51&west=0&east=181")]
    [InlineData("/aircraft/in-view?south=50&north=51&west=0&east=1&limit=0")]
    [InlineData("/aircraft/in-view?south=50&north=51&west=0&east=1&limit=5001")]
    [InlineData("/aircraft/in-view?south=50&north=51&west=0&east=1&kinds=spaceship")]
    [InlineData("/aircraft/in-view?south=-60&north=70&west=-170&east=170")] // the whole world is too big
    public async Task In_view_rejects_missing_invalid_or_oversized_views(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync(url)).StatusCode);

    [Theory]
    [InlineData("/aircraft/zzz")]
    [InlineData("/aircraft/12345")]
    [InlineData("/aircraft/1234567")]
    [InlineData("/aircraft/gggggg")]
    public async Task Detail_rejects_a_malformed_hex(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync(url)).StatusCode);
}
