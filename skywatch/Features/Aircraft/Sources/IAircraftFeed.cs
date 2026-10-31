using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

public interface IAircraftFeed
{
    // Aircraft near a point. Served from cache when fresh, otherwise fetched from the first source that answers.
    // "Fresh" means younger than maxAge, or than the configured CacheTtl when maxAge is not given.
    Task<AircraftSnapshot> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct,
        TimeSpan? maxAge = null);

    Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct);
}
