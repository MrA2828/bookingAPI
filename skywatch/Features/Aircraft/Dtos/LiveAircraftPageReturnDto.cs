namespace SkyWatch.Features.Aircraft.Dtos;

// FetchedAt is when the positions were read from Source. Stale is true when every source was down and
// FetchedAt is the last good reading.
public record LiveAircraftPageReturnDto(string Source, DateTime FetchedAt, bool Stale, int TotalCount, int PageNumber, int PageSize,
    IReadOnlyList<AircraftReturnDto> Items);
