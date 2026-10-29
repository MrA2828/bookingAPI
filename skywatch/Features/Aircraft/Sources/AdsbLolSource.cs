namespace SkyWatch.Features.Aircraft.Sources;

// Primary source: free, no key, community ADS-B network (api.adsb.lol).
public class AdsbLolSource(HttpClient http) : AdsbV2Source(http)
{
    public override string Name => "adsb.lol";
    protected override string AroundPath(string latitude, string longitude, int radiusNauticalMiles) => $"v2/point/{latitude}/{longitude}/{radiusNauticalMiles}";
    protected override string HexPath(string hex) => $"v2/hex/{hex}";
}
