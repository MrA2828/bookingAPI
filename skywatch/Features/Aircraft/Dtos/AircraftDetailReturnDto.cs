using SkyWatch.Features.Airports.Dtos;

namespace SkyWatch.Features.Aircraft.Dtos;

// Route is null when the callsign is unknown to the route database (common for private and military flights).
public record AircraftDetailReturnDto(AircraftReturnDto Aircraft, AirlineReturnDto? Airline, FlightRouteReturnDto? Route);
