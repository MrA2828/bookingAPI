using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace SkyWatch.IntegrationTests;

// A host with a tiny limit, so the 429 can be reached without hundreds of requests.
public class RateLimitedFactory : SkyWatchFactory
{
    public const int PermitLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimit:PermitLimit", PermitLimit.ToString());
    }
}

public class RateLimitTests(RateLimitedFactory api) : IClassFixture<RateLimitedFactory>
{
    [Fact]
    public async Task Requests_over_the_limit_get_429_with_retry_after()
    {
        for (var i = 0; i < RateLimitedFactory.PermitLimit; i++)
            Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/reference-data/status")).StatusCode);

        var rejected = await api.Client.GetAsync("/reference-data/status");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task Swagger_and_the_openapi_document_are_not_limited()
    {
        for (var i = 0; i < RateLimitedFactory.PermitLimit + 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await api.Anonymous.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
