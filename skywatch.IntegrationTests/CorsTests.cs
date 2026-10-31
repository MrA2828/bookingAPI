using System.Net;

namespace SkyWatch.IntegrationTests;

public class CorsTests(SkyWatchFactory api) : IClassFixture<SkyWatchFactory>
{
    private const string AllowedOrigin = "http://localhost:5173";

    [Fact]
    public async Task Preflight_from_the_web_client_is_answered_without_a_token()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/aircraft");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        var response = await api.Anonymous.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Other_origins_get_no_cors_headers()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/aircraft");
        request.Headers.Add("Origin", "http://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await api.Anonymous.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Retry_after_is_exposed_to_the_browser()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await api.Anonymous.SendAsync(request);

        Assert.Contains("Retry-After", response.Headers.GetValues("Access-Control-Expose-Headers").Single());
    }
}
