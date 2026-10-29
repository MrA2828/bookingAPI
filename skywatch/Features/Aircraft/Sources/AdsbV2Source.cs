using System.Globalization;
using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

public abstract class AdsbV2Source(HttpClient http) : IAircraftSource
{
    public abstract string Name { get; }
    protected abstract string AroundPath(string latitude, string longitude, int radiusNauticalMiles);
    protected abstract string HexPath(string hex);

    public async Task<IReadOnlyList<AircraftState>> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct) =>
        await GetAsync(AroundPath(
            latitude.ToString("0.####", CultureInfo.InvariantCulture),
            longitude.ToString("0.####", CultureInfo.InvariantCulture),
            radiusNauticalMiles), ct);

    public async Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct) =>
        (await GetAsync(HexPath(hex.ToLowerInvariant()), ct)).FirstOrDefault();

    private async Task<IReadOnlyList<AircraftState>> GetAsync(string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(ct);
        return AdsbV2Parser.Parse(body);
    }
}
