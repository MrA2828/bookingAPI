namespace SkyWatch.Cors;

public static class CorsExtensions
{
    private const string PolicyName = "web-client";

    public static IServiceCollection AddWebClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsOptions.Section).Get<CorsOptions>()?.AllowedOrigins ?? [];
        services.AddCors(cors => cors.AddPolicy(PolicyName, policy =>
        {
            if (origins.Length == 0) return;
            // Retry-After has to be exposed, otherwise the browser hides it from the client on a 429.
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After");
        }));
        return services;
    }

    public static IApplicationBuilder UseWebClientCors(this IApplicationBuilder app) => app.UseCors(PolicyName);
}
