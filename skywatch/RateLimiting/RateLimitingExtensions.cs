using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace SkyWatch.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.Section));
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteRejectionAsync;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var options = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                var path = context.Request.Path;
                if (!options.Enabled || path.StartsWithSegments("/swagger") || path.StartsWithSegments("/openapi"))
                    return RateLimitPartition.GetNoLimiter("unlimited");

                return RateLimitPartition.GetSlidingWindowLimiter(PartitionKey(context), _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = options.Window,
                    SegmentsPerWindow = options.Segments,
                    QueueLimit = 0
                });
            });
        });
        return services;
    }

    // Signed-in users are counted per account, everyone else per IP address.
    private static string PartitionKey(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";

    private static async ValueTask WriteRejectionAsync(OnRejectedContext rejected, CancellationToken ct)
    {
        var context = rejected.HttpContext;
        var wait = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? retryAfter
            : context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value.Window;
        context.Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Request limit reached, retry after the time in the Retry-After header."
        }, ct);
    }
}
