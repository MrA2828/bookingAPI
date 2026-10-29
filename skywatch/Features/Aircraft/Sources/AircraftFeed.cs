using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

// Tries the sources in registration order (adsb.lol, then adsb.fi) and keeps what they return in memory,
// so a burst of requests for the same area costs one upstream call. If every source fails, the last good
// copy is returned flagged as stale; only when there is no copy at all does the call fail.
public class AircraftFeed(
    IEnumerable<IAircraftSource> sources, IMemoryCache cache, IOptions<AircraftOptions> options, ILogger<AircraftFeed> logger)
    : IAircraftFeed
{
    // The upstream APIs cap the search radius at 250 nautical miles.
    private const int MaxRadiusNauticalMiles = 250;
    // The centre is snapped to a 0.1 degree grid (up to ~8 nm off), so the radius is widened by that much.
    private const int SnapMarginNauticalMiles = 8;

    private readonly AircraftOptions _options = options.Value;

    public Task<AircraftSnapshot> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct)
    {
        var centreLatitude = Math.Round(latitude, 1);
        var centreLongitude = Math.Round(longitude, 1);
        var radius = Math.Min(radiusNauticalMiles + SnapMarginNauticalMiles, MaxRadiusNauticalMiles);
        var key = string.Create(CultureInfo.InvariantCulture, $"around:{centreLatitude:0.0}:{centreLongitude:0.0}:{radius}");
        return GetSnapshotAsync(key, source => source.GetAroundAsync(centreLatitude, centreLongitude, radius, ct), ct);
    }

    public async Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct)
    {
        var snapshot = await GetSnapshotAsync($"hex:{hex.ToLowerInvariant()}",
            async source => await source.GetByHexAsync(hex, ct) is { } state ? [state] : [], ct);
        return snapshot.Aircraft.FirstOrDefault();
    }

    private async Task<AircraftSnapshot> GetSnapshotAsync(
        string key, Func<IAircraftSource, Task<IReadOnlyList<AircraftState>>> fetch, CancellationToken ct)
    {
        cache.TryGetValue(key, out AircraftSnapshot? cached);
        if (cached is not null && DateTime.UtcNow - cached.FetchedAt < _options.CacheTtl)
            return cached;

        Exception? lastError = null;
        foreach (var source in sources)
        {
            try
            {
                var aircraft = await fetch(source);
                var snapshot = new AircraftSnapshot(aircraft, DateTime.UtcNow, source.Name, Stale: false);
                cache.Set(key, snapshot, _options.StaleTtl);
                return snapshot;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Aircraft source {Source} failed for {Key}", source.Name, key);
                lastError = ex;
            }
        }

        if (cached is not null)
        {
            logger.LogWarning("All aircraft sources failed for {Key}; serving a snapshot from {FetchedAt:u}", key, cached.FetchedAt);
            return cached with { Stale = true };
        }
        throw new AircraftFeedUnavailableException("No live aircraft source is reachable right now", lastError);
    }
}
