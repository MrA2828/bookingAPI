using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;
using SkyWatch.Services;

namespace SkyWatch.Features.Airports.Queries;

public record NearbyAirportsQuery(double Latitude, double Longitude, double RadiusKm, int PageNumber, int PageSize, bool ScheduledOnly)
    : IRequest<IReadOnlyList<NearbyAirportDto>>;

public class NearbyAirportsHandler(IUnitOfWork uow) : IRequestHandler<NearbyAirportsQuery, IReadOnlyList<NearbyAirportDto>>
{
    public async Task<IReadOnlyList<NearbyAirportDto>> Handle(NearbyAirportsQuery r, CancellationToken ct)
    {
        // Cheap bounding-box filter in SQL, exact great-circle distance in memory.
        var latDelta = r.RadiusKm / 111.0;
        var lonDelta = r.RadiusKm / (111.0 * Math.Max(0.01, Math.Cos(r.Latitude * Math.PI / 180)));
        var candidates = await uow.Airports.InBoundingBoxAsync(
            r.Latitude - latDelta, r.Latitude + latDelta, r.Longitude - lonDelta, r.Longitude + lonDelta, r.ScheduledOnly, ct);

        return candidates
            .Select(a => new NearbyAirportDto(AirportDto.From(a), Math.Round(Geo.DistanceKm(r.Latitude, r.Longitude, a.Latitude, a.Longitude), 1)))
            .Where(n => n.DistanceKm <= r.RadiusKm)
            .OrderBy(n => n.DistanceKm)
            .Skip((Math.Max(r.PageNumber, 1) - 1) * Math.Clamp(r.PageSize, 1, 100))
            .Take(Math.Clamp(r.PageSize, 1, 100))
            .ToList();
    }
}
