namespace SkyWatch.Features.Aircraft.Models;

public record RouteAirport(string? IataCode, string? IcaoCode, string Name, string? Municipality, string? CountryCode,
    double Latitude, double Longitude);
