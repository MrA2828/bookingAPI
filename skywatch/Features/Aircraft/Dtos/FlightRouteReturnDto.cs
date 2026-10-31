namespace SkyWatch.Features.Aircraft.Dtos;

// Path is the planned great-circle line from Origin to Destination (the shortest route; real flight plans
// follow airways, so it is a guide, not the filed route). The estimated arrival is straight-line distance to
// the destination divided by the current ground speed, so it is rough during climb and descent.
public record FlightRouteDto(RouteAirportDto? Origin, RouteAirportDto? Destination, double? TotalDistanceKm,
    double? DistanceToDestinationKm, DateTime? EstimatedArrival, IReadOnlyList<GeoPointDto> Path);
