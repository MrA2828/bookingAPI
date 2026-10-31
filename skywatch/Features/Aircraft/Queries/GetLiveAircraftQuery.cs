using MediatR;
using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;
using SkyWatch.Services;

namespace SkyWatch.Features.Aircraft.Queries;

public record GetLiveAircraftQuery(LiveAircraftRequestDto Request) : IRequest<LiveAircraftPageReturnDto>;

public class GetLiveAircraftHandler(IAircraftFeed feed, IAirlineCodes airlineCodes)
    : IRequestHandler<GetLiveAircraftQuery, LiveAircraftPageReturnDto>
{
    // Military and private aircraft are left out unless asked for.
    private static readonly AircraftKind[] DefaultKinds = [AircraftKind.Commercial, AircraftKind.Unknown];

    public async Task<LiveAircraftPageReturnDto> Handle(GetLiveAircraftQuery r, CancellationToken ct)
    {
        var radiusNauticalMiles = (int)Math.Ceiling(r.Request.RadiusKm / Geo.KilometresPerNauticalMile);
        var snapshot = await feed.GetAroundAsync(r.Request.Latitude, r.Request.Longitude, radiusNauticalMiles, ct);
        var airlineIcaoCodes = await airlineCodes.GetIcaoCodesAsync(ct);
        var kinds = (r.Request.Kinds is { Length: > 0 } ? r.Request.Kinds : DefaultKinds).ToHashSet();

        var matches = snapshot.Aircraft
            .Select(a => (State: a, Kind: AircraftClassifier.Classify(a, airlineIcaoCodes),
                Distance: Geo.DistanceKm(r.Request.Latitude, r.Request.Longitude, a.Latitude, a.Longitude)))
            .Where(x => x.Distance <= r.Request.RadiusKm
                && kinds.Contains(x.Kind)
                && (r.Request.IncludeOnGround || !x.State.OnGround)
                && (r.Request.MinAltitudeFt is not { } min || x.State.AltitudeFt >= min)
                && (r.Request.MaxAltitudeFt is not { } max || x.State.AltitudeFt <= max))
            .OrderBy(x => x.Distance)
            .ToList();

        var pageNumber = Math.Max(r.Request.PageNumber, 1);
        var pageSize = Math.Clamp(r.Request.PageSize, 1, 100);
        var items = matches.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => AircraftReturnDto.From(x.State, x.Kind, x.Distance))
            .ToList();
        return new LiveAircraftPageReturnDto(snapshot.Source, snapshot.FetchedAt, snapshot.Stale, matches.Count, pageNumber, pageSize, items);
    }
}
