namespace SkyWatch.Features.Airports.Dtos;

public record AirlineDto(int Id, string Name, string? IataCode, string? IcaoCode, string? Callsign, string? Country);
