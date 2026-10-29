using MediatR;
using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;
using SkyWatch.Services;

namespace SkyWatch.Features.Aircraft.Queries;

public record GetAircraftQuery(string Hex) : IRequest<AircraftDetailDto?>;

public class GetAircraftHandler(IAircraftFeed feed, IFlightRouteProvider routes, IAirlineCodes airlineCodes, IUnitOfWork uow)
    : IRequestHandler<GetAircraftQuery, AircraftDetailDto?>
{
    private const int PathSegments = 48;
    // Below this the aircraft is taxiing or hovering and a speed-based ETA means nothing.
    private const int MinSpeedForEtaKt = 60;

    public async Task<AircraftDetailDto?> Handle(GetAircraftQuery r, CancellationToken ct)
    {
        if (await feed.GetByHexAsync(r.Hex, ct) is not { } state) return null;

        var kind = AircraftClassifier.Classify(state, await airlineCodes.GetIcaoCodesAsync(ct));

        // Only airline-style callsigns are in the route database; skip the lookup (and its rate limit) otherwise.
        var airline = kind == AircraftKind.Commercial && state.Callsign is { } callsign
            ? await uow.Airlines.FindByIcaoCodeAsync(callsign[..3], ct) : null;
        var route = kind != AircraftKind.Military && state.Callsign is { } flight
            ? await routes.GetRouteAsync(flight, ct) : null;

        return new AircraftDetailDto(
            AircraftDto.From(state, kind),
            airline is null ? null : new AirlineDto(airline.Id, airline.Name, airline.IataCode, airline.IcaoCode, airline.Callsign, airline.Country),
            route is null ? null : BuildRoute(state, route));
    }

    private static FlightRouteDto BuildRoute(AircraftState state, FlightRouteInfo route)
    {
        var origin = route.Origin;
        var destination = route.Destination;

        double? total = origin is not null && destination is not null
            ? Geo.DistanceKm(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude) : null;
        double? remaining = destination is not null
            ? Geo.DistanceKm(state.Latitude, state.Longitude, destination.Latitude, destination.Longitude) : null;

        DateTime? arrival = remaining is { } km && !state.OnGround && state.GroundSpeedKt is >= MinSpeedForEtaKt
            ? DateTime.UtcNow.AddHours(km / (state.GroundSpeedKt.Value * Geo.KilometresPerNauticalMile)) : null;

        // Planned line: origin to destination; if the origin is unknown, from where the aircraft is now.
        IReadOnlyList<GeoPointDto> path = [];
        if (destination is not null)
        {
            var (fromLatitude, fromLongitude) = origin is null ? (state.Latitude, state.Longitude) : (origin.Latitude, origin.Longitude);
            path = Geo.GreatCirclePath(fromLatitude, fromLongitude, destination.Latitude, destination.Longitude, PathSegments)
                .Select(p => new GeoPointDto(Math.Round(p.Latitude, 4), Math.Round(p.Longitude, 4)))
                .ToList();
        }

        return new FlightRouteDto(
            origin is null ? null : RouteAirportDto.From(origin),
            destination is null ? null : RouteAirportDto.From(destination),
            total is { } t ? Math.Round(t, 1) : null,
            remaining is { } d ? Math.Round(d, 1) : null,
            arrival,
            path);
    }
}
