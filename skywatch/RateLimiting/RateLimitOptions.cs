namespace SkyWatch.RateLimiting;

public class RateLimitOptions
{
    public const string Section = "RateLimit";

    public bool Enabled { get; set; } = true;

    // Each signed-in user (or, for anonymous calls such as /auth/*, each client IP) gets PermitLimit requests
    // per Window. The window slides in Segments steps, so a burst at a window boundary cannot double the limit.
    public int PermitLimit { get; set; } = 120;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    public int Segments { get; set; } = 6;
}
