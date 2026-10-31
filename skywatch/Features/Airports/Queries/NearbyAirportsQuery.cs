using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;
using SkyWatch.Services;

namespace SkyWatch.Features.Airports.Queries;

public record NearbyAirportsQuery(NearbyAirportsRequestDto Request)
    : IRequest<IReadOnlyList<NearbyAirportReturnDto>>;

public class NearbyAirportsHandler(IUnitOfWork uow) : IRequestHandler<NearbyAirportsQuery, IReadOnlyList<NearbyAirportReturnDto>>
{
    public async Task<IReadOnlyList<NearbyAirportReturnDto>> Handle(NearbyAirportsQuery r, CancellationToken ct)
    {
        // Cheap bounding-box filter in SQL, exact great-circle distance in memory.
        var latDelta = r.Request.RadiusKm / 111.0;
        var lonDelta = r.Request.RadiusKm / (111.0 * Math.Max(0.01, Math.Cos(r.Request.Latitude * Math.PI / 180)));
        var candidates = await uow.Airports.InBoundingBoxAsync(
            r.Request.Latitude - latDelta, r.Request.Latitude + latDelta, r.Request.Longitude - lonDelta, r.Request.Longitude + lonDelta, r.Request.ScheduledOnly, ct);

        return candidates
            .Select(a => new NearbyAirportReturnDto(AirportReturnDto.From(a), Math.Round(Geo.DistanceKm(r.Request.Latitude, r.Request.Longitude, a.Latitude, a.Longitude), 1)))
            .Where(n => n.DistanceKm <= r.Request.RadiusKm)
            .OrderBy(n => n.DistanceKm)
            .Skip((Math.Max(r.Request.PageNumber, 1) - 1) * Math.Clamp(r.Request.PageSize, 1, 100))
            .Take(Math.Clamp(r.Request.PageSize, 1, 100))
            .ToList();
    }
}
