using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace SkyWatch.Features.Aircraft.Sources;

public static class AircraftHttpResilience
{
    // Retry with backoff and jitter, each attempt under its own timeout. The HttpClient's own timeout is
    // switched off (see Program.cs) because it would cover all attempts together.
    public static IHttpClientBuilder AddAircraftResilience(this IHttpClientBuilder client)
    {
        client.AddResilienceHandler("aircraft", (pipeline, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<AircraftOptions>>().Value;

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.RetryCount,
                Delay = options.RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(IsTransient(args.Outcome))
            });
            pipeline.AddTimeout(options.RequestTimeout);
        });
        return client;
    }

    private static bool IsTransient(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Exception: HttpRequestException or TimeoutRejectedException } => true,
        { Result: { } response } => (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout,
        _ => false
    };
}
