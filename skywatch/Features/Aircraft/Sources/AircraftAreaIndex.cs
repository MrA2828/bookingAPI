using SkyWatch.Services;

namespace SkyWatch.Features.Aircraft.Sources;

// Remembers which circles were fetched recently (by cache key), so a request whose circle lies entirely inside
// one of them can be answered from that cached snapshot instead of calling a source. Registered as a singleton.
// It only holds geometry; the aircraft stay in the memory cache, which decides when an area has expired.
public class AircraftAreaIndex
{
    private const int MaxAreas = 200;

    private readonly Dictionary<string, Area> _areas = new();
    private long _sequence;

    private sealed record Area(string Key, double Latitude, double Longitude, int RadiusNauticalMiles, long Sequence);

    public void Add(string key, double latitude, double longitude, int radiusNauticalMiles)
    {
        lock (_areas)
        {
            _areas[key] = new Area(key, latitude, longitude, radiusNauticalMiles, ++_sequence);
            if (_areas.Count > MaxAreas)
                _areas.Remove(_areas.Values.MinBy(a => a.Sequence)!.Key);
        }
    }

    public void Remove(string key)
    {
        lock (_areas) _areas.Remove(key);
    }

    // Keys of the areas that contain the given circle, smallest first (the fewest aircraft to filter).
    public IReadOnlyList<string> FindCovering(double latitude, double longitude, int radiusNauticalMiles)
    {
        Area[] areas;
        lock (_areas) areas = _areas.Values.ToArray();

        return areas
            .Where(a => a.RadiusNauticalMiles >= radiusNauticalMiles
                && Geo.DistanceKm(a.Latitude, a.Longitude, latitude, longitude) / Geo.KilometresPerNauticalMile
                    + radiusNauticalMiles <= a.RadiusNauticalMiles)
            .OrderBy(a => a.RadiusNauticalMiles)
            .Select(a => a.Key)
            .ToList();
    }
}
