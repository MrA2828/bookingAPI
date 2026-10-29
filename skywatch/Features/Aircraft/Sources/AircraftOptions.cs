namespace SkyWatch.Features.Aircraft.Sources;

public class AircraftOptions
{
    public const string Section = "Aircraft";

    public string AdsbLolUrl { get; set; } = "https://api.adsb.lol";
    public string AdsbFiUrl { get; set; } = "https://opendata.adsb.fi";
    public string AdsbdbUrl { get; set; } = "https://api.adsbdb.com";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(8);

    // Positions are served from memory for CacheTtl. If every source is down, a copy up to StaleTtl old
    // is returned (flagged stale) rather than an error.
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan StaleTtl { get; set; } = TimeSpan.FromMinutes(10);

    // adsbdb asks consumers to cache routes only briefly.
    public TimeSpan RouteCacheTtl { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan UnknownRouteCacheTtl { get; set; } = TimeSpan.FromMinutes(2);
}
