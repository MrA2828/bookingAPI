using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

public interface IAircraftSource
{
    string Name { get; }
    Task<IReadOnlyList<AircraftState>> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct);
    Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct);
}
