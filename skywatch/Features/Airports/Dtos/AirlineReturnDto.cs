namespace SkyWatch.Features.Airports.Dtos;

public record AirlineReturnDto(int Id, string Name, string? IataCode, string? IcaoCode, string? Callsign, string? Country);
