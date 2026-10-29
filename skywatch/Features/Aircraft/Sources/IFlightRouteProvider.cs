using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

public interface IFlightRouteProvider
{
    // Origin and destination for a flight callsign, or null when the route is unknown.
    Task<FlightRouteInfo?> GetRouteAsync(string callsign, CancellationToken ct);
}
