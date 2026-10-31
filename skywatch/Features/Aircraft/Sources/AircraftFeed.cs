using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Services;

namespace SkyWatch.Features.Aircraft.Sources;

// Tries the sources in registration order (adsb.lol, then adsb.fi) and keeps what they return in memory,
// so a burst of requests for the same area costs one upstream call (a per-key lock makes the others wait for it). If every source fails, the last good
// copy is returned flagged as stale; only when there is no copy at all does the call fail.
public class AircraftFeed(
    IEnumerable<IAircraftSource> sources, IMemoryCache cache, KeyedLock locks, AircraftAreaIndex areas, IOptions<AircraftOptions> options,
    ILogger<AircraftFeed> logger)
    : IAircraftFeed
{
    // The upstream APIs cap the search radius at 250 nautical miles.
    private const int MaxRadiusNauticalMiles = 250;
    // The centre is snapped to a 0.1 degree grid (up to ~8 nm off), so the radius is widened by that much.
    private const int SnapMarginNauticalMiles = 8;

    private readonly AircraftOptions _options = options.Value;

    public async Task<AircraftSnapshot> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct)
    {
        // "+ 0.0" turns negative zero into zero, otherwise -0.02 and 0.02 would get different cache keys.
        var centreLatitude = Math.Round(latitude, 1) + 0.0;
        var centreLongitude = Math.Round(longitude, 1) + 0.0;
        var radius = Math.Min(radiusNauticalMiles + SnapMarginNauticalMiles, MaxRadiusNauticalMiles);
        var key = string.Create(CultureInfo.InvariantCulture, $"around:{centreLatitude:0.0}:{centreLongitude:0.0}:{radius}");

        if (TryGetFresh(key, out var exact)) return exact!;
        if (TryGetFromCoveringArea(centreLatitude, centreLongitude, radius, out var covering)) return covering!;

        var snapshot = await GetSnapshotAsync(key, source => source.GetAroundAsync(centreLatitude, centreLongitude, radius, ct), ct);
        if (!snapshot.Stale) areas.Add(key, centreLatitude, centreLongitude, radius);
        return snapshot;
    }

    // A fresh snapshot of a larger area that contains this circle is as good as fetching it again: take the
    // aircraft inside the circle from it. Areas whose snapshot has left the cache are dropped from the index.
    private bool TryGetFromCoveringArea(double latitude, double longitude, int radiusNauticalMiles, out AircraftSnapshot? result)
    {
        foreach (var coveringKey in areas.FindCovering(latitude, longitude, radiusNauticalMiles))
        {
            if (!cache.TryGetValue(coveringKey, out AircraftSnapshot? snapshot) || snapshot is null)
            {
                areas.Remove(coveringKey);
                continue;
            }
            if (DateTime.UtcNow - snapshot.FetchedAt >= _options.CacheTtl) continue;

            var radiusKm = radiusNauticalMiles * Geo.KilometresPerNauticalMile;
            result = snapshot with
            {
                Aircraft = snapshot.Aircraft.Where(a => Geo.DistanceKm(latitude, longitude, a.Latitude, a.Longitude) <= radiusKm).ToList()
            };
            return true;
        }
        result = null;
        return false;
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
        if (TryGetFresh(key, out var cached)) return cached!;

        // Concurrent requests for the same key wait here; the first one fetches and the rest find it in the cache.
        using var _ = await locks.AcquireAsync(key, ct);
        if (TryGetFresh(key, out cached)) return cached!;

        // If every source just failed for this key, the requests that queued behind that attempt do not try again.
        var recentlyFailed = cache.TryGetValue(FailureKey(key), out Exception? lastError);
        foreach (var source in recentlyFailed ? Enumerable.Empty<IAircraftSource>() : sources)
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

        if (!recentlyFailed) cache.Set(FailureKey(key), lastError, _options.FailureTtl);
        if (cached is not null)
        {
            logger.LogWarning("All aircraft sources failed for {Key}; serving a snapshot from {FetchedAt:u}", key, cached.FetchedAt);
            return cached with { Stale = true };
        }
        throw new AircraftFeedUnavailableException("No live aircraft source is reachable right now", lastError);
    }

    private static string FailureKey(string key) => $"failed:{key}";

    // Returns true only for a fresh copy; "cached" is the stale copy (if any) otherwise.
    private bool TryGetFresh(string key, out AircraftSnapshot? cached)
    {
        cache.TryGetValue(key, out cached);
        return cached is not null && DateTime.UtcNow - cached.FetchedAt < _options.CacheTtl;
    }
}
