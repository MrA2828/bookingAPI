namespace SkyWatch.Features.Aircraft.Models;

// What a source returned for an area, plus where and when it came from. Stale means every source failed
// and this is the last good copy.
public record AircraftSnapshot(IReadOnlyList<AircraftState> Aircraft, DateTime FetchedAt, string Source, bool Stale);
