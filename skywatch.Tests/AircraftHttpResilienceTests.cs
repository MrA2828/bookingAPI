using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class AircraftHttpResilienceTests
{
    private sealed class ScriptedHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public int Calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(statuses[Math.Min(Calls++, statuses.Length - 1)]));
    }

    private static HttpClient Client(ScriptedHandler handler, int retries = 2)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new AircraftOptions { RetryCount = retries, RetryDelay = TimeSpan.FromMilliseconds(1) }));
        services.AddHttpClient("test", c => c.BaseAddress = new Uri("http://localhost/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddAircraftResilience();
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("test");
    }

    [Fact]
    public async Task Retries_a_server_error_until_it_succeeds()
    {
        var handler = new ScriptedHandler(HttpStatusCode.BadGateway, HttpStatusCode.OK);
        var response = await Client(handler).GetAsync("x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task Gives_up_after_the_configured_number_of_retries()
    {
        var handler = new ScriptedHandler(HttpStatusCode.ServiceUnavailable);
        var response = await Client(handler, retries: 2).GetAsync("x");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(3, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Does_not_retry_rate_limits_or_client_errors(HttpStatusCode status)
    {
        var handler = new ScriptedHandler(status);
        await Client(handler).GetAsync("x");

        Assert.Equal(1, handler.Calls);
    }
}
