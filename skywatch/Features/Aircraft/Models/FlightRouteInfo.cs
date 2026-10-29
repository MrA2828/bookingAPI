namespace SkyWatch.Features.Aircraft.Models;

public record FlightRouteInfo(string Callsign, string? AirlineName, RouteAirport? Origin, RouteAirport? Destination);
