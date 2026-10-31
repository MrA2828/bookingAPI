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
    [InlineData("/aircraft/zzz")]
    [InlineData("/aircraft/12345")]
    [InlineData("/aircraft/1234567")]
    [InlineData("/aircraft/gggggg")]
    public async Task Detail_rejects_a_malformed_hex(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync(url)).StatusCode);
}
