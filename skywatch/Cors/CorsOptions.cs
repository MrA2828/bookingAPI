namespace SkyWatch.Cors;

public class CorsOptions
{
    public const string Section = "Cors";

    // Browser apps served from these origins may call the API (e.g. the SkyWatch web client in development).
    // Empty means no browser origin is allowed.
    public string[] AllowedOrigins { get; set; } = [];
}
