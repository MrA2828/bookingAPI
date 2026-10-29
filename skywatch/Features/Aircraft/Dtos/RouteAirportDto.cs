using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Dtos;

public record RouteAirportDto(string? IataCode, string? IcaoCode, string Name, string? Municipality, string? CountryCode,
    double Latitude, double Longitude)
{
    public static RouteAirportDto From(RouteAirport a) => new(a.IataCode, a.IcaoCode, a.Name, a.Municipality, a.CountryCode, a.Latitude, a.Longitude);
}
