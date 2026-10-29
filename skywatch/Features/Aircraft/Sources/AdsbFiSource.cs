namespace SkyWatch.Features.Aircraft.Sources;

// Fallback source: adsb.fi open data, same data format as adsb.lol.
public class AdsbFiSource(HttpClient http) : AdsbV2Source(http)
{
    public override string Name => "adsb.fi";
    protected override string AroundPath(string latitude, string longitude, int radiusNauticalMiles) => $"api/v2/lat/{latitude}/lon/{longitude}/dist/{radiusNauticalMiles}";
    protected override string HexPath(string hex) => $"api/v2/hex/{hex}";
}
