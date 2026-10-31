namespace SkyWatch.Features.Aircraft.Dtos;

// Source lists the sources that answered (a wide view is several calls). FetchedAt is the oldest reading among them.
// Stale is true when any part of the view is the last good copy. Complete is false when part of the view could not
// be read at all, so the aircraft there are missing. TotalCount is every match; Items holds at most `limit` of them,
// highest altitude first.
public record AircraftInViewReturnDto(string Source, DateTime FetchedAt, bool Stale, bool Complete, int TotalCount,
    IReadOnlyList<AircraftReturnDto> Items);
