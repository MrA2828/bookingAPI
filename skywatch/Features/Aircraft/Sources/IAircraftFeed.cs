using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

public interface IAircraftFeed
{
    // Aircraft near a point. Served from cache when fresh, otherwise fetched from the first source that answers.
    Task<AircraftSnapshot> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct);

    Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct);
}
