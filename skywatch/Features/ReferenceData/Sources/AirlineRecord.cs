namespace SkyWatch.Features.ReferenceData.Sources;

public record AirlineRecord(
    int Id, string Name, string? Alias, string? IataCode, string? IcaoCode, string? Callsign, string? Country);
