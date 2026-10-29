using MediatR;
using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;
using SkyWatch.Services;

namespace SkyWatch.Features.Aircraft.Queries;

public record GetLiveAircraftQuery(double Latitude, double Longitude, double RadiusKm, IReadOnlyList<AircraftKind>? Kinds,
    int? MinAltitudeFt, int? MaxAltitudeFt, bool IncludeOnGround, int PageNumber, int PageSize) : IRequest<LiveAircraftPageDto>;

public class GetLiveAircraftHandler(IAircraftFeed feed, IAirlineCodes airlineCodes)
    : IRequestHandler<GetLiveAircraftQuery, LiveAircraftPageDto>
{
    // Military and private aircraft are left out unless asked for.
    private static readonly AircraftKind[] DefaultKinds = [AircraftKind.Commercial, AircraftKind.Unknown];

    public async Task<LiveAircraftPageDto> Handle(GetLiveAircraftQuery r, CancellationToken ct)
    {
        var radiusNauticalMiles = (int)Math.Ceiling(r.RadiusKm / Geo.KilometresPerNauticalMile);
        var snapshot = await feed.GetAroundAsync(r.Latitude, r.Longitude, radiusNauticalMiles, ct);
        var airlineIcaoCodes = await airlineCodes.GetIcaoCodesAsync(ct);
        var kinds = (r.Kinds is { Count: > 0 } ? r.Kinds : DefaultKinds).ToHashSet();

        var matches = snapshot.Aircraft
            .Select(a => (State: a, Kind: AircraftClassifier.Classify(a, airlineIcaoCodes),
                Distance: Geo.DistanceKm(r.Latitude, r.Longitude, a.Latitude, a.Longitude)))
            .Where(x => x.Distance <= r.RadiusKm
                && kinds.Contains(x.Kind)
                && (r.IncludeOnGround || !x.State.OnGround)
                && (r.MinAltitudeFt is not { } min || x.State.AltitudeFt >= min)
                && (r.MaxAltitudeFt is not { } max || x.State.AltitudeFt <= max))
            .OrderBy(x => x.Distance)
            .ToList();

        var pageNumber = Math.Max(r.PageNumber, 1);
        var pageSize = Math.Clamp(r.PageSize, 1, 100);
        var items = matches.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => AircraftDto.From(x.State, x.Kind, x.Distance))
            .ToList();
        return new LiveAircraftPageDto(snapshot.Source, snapshot.FetchedAt, snapshot.Stale, matches.Count, pageNumber, pageSize, items);
    }
}
