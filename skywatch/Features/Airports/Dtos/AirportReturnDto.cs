using SkyWatch.Entities;

namespace SkyWatch.Features.Airports.Dtos;

public record AirportReturnDto(int Id, string Ident, string Name, string Type, string? IataCode, string? IcaoCode,
    double Latitude, double Longitude, int? ElevationFt, string? Municipality, string CountryCode, string Continent,
    bool ScheduledService)
{
    public static AirportReturnDto From(Airport a) => new(a.Id, a.Ident, a.Name, a.Type, a.IataCode, a.IcaoCode,
        a.Latitude, a.Longitude, a.ElevationFt, a.Municipality, a.CountryCode, a.Continent, a.ScheduledService);
}
