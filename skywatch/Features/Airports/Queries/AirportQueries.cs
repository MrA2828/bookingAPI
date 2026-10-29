using MediatR;
using SkyWatch.Entities;
using SkyWatch.Repositories;
using SkyWatch.Services;

namespace SkyWatch.Features.Airports.Queries;

public record AirportDto(int Id, string Ident, string Name, string Type, string? IataCode, string? IcaoCode,
    double Latitude, double Longitude, int? ElevationFt, string? Municipality, string CountryCode, string Continent,
    bool ScheduledService)
{
    public static AirportDto From(Airport a) => new(a.Id, a.Ident, a.Name, a.Type, a.IataCode, a.IcaoCode,
        a.Latitude, a.Longitude, a.ElevationFt, a.Municipality, a.CountryCode, a.Continent, a.ScheduledService);
}

public record NearbyAirportDto(AirportDto Airport, double DistanceKm);

public record AirlineDto(int Id, string Name, string? IataCode, string? IcaoCode, string? Callsign, string? Country);

public record SearchAirportsQuery(string? Q, string? Country, string? Type, int Limit, int Offset) : IRequest<IReadOnlyList<AirportDto>>;

public record GetAirportQuery(string Code) : IRequest<AirportDto?>;

public record NearbyAirportsQuery(double Latitude, double Longitude, double RadiusKm, int Limit, bool ScheduledOnly)
    : IRequest<IReadOnlyList<NearbyAirportDto>>;

public record SearchAirlinesQuery(string? Q, int Limit, int Offset) : IRequest<IReadOnlyList<AirlineDto>>;

public class SearchAirportsHandler(IUnitOfWork uow) : IRequestHandler<SearchAirportsQuery, IReadOnlyList<AirportDto>>
{
    public async Task<IReadOnlyList<AirportDto>> Handle(SearchAirportsQuery r, CancellationToken ct) =>
        (await uow.Airports.SearchAsync(r.Q, r.Country, r.Type, Math.Clamp(r.Limit, 1, 100), Math.Max(r.Offset, 0), ct))
            .Select(AirportDto.From).ToList();
}

public class GetAirportHandler(IUnitOfWork uow) : IRequestHandler<GetAirportQuery, AirportDto?>
{
    public async Task<AirportDto?> Handle(GetAirportQuery r, CancellationToken ct) =>
        await uow.Airports.FindByCodeAsync(r.Code, ct) is { } airport ? AirportDto.From(airport) : null;
}

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
            .Take(Math.Clamp(r.Limit, 1, 100))
            .ToList();
    }
}

public class SearchAirlinesHandler(IUnitOfWork uow) : IRequestHandler<SearchAirlinesQuery, IReadOnlyList<AirlineDto>>
{
    public async Task<IReadOnlyList<AirlineDto>> Handle(SearchAirlinesQuery r, CancellationToken ct) =>
        (await uow.Airlines.SearchAsync(r.Q, Math.Clamp(r.Limit, 1, 100), Math.Max(r.Offset, 0), ct))
            .Select(a => new AirlineDto(a.Id, a.Name, a.IataCode, a.IcaoCode, a.Callsign, a.Country)).ToList();
}
