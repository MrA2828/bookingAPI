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
    public async Task<LiveAircraftPageReturnDto> Handle(GetLiveAircraftQuery r, CancellationToken ct)
    {
        var radiusNauticalMiles = (int)Math.Ceiling(r.Request.RadiusKm / Geo.KilometresPerNauticalMile);
        var snapshot = await feed.GetAroundAsync(r.Request.Latitude, r.Request.Longitude, radiusNauticalMiles, ct);
        var airlineIcaoCodes = await airlineCodes.GetIcaoCodesAsync(ct);
        var filter = new AircraftFilter(r.Request.Kinds, r.Request.IncludeOnGround, r.Request.MinAltitudeFt, r.Request.MaxAltitudeFt);

        var matches = snapshot.Aircraft
            .Select(a => (State: a, Kind: AircraftClassifier.Classify(a, airlineIcaoCodes),
                Distance: Geo.DistanceKm(r.Request.Latitude, r.Request.Longitude, a.Latitude, a.Longitude)))
            .Where(x => x.Distance <= r.Request.RadiusKm && filter.Matches(x.State, x.Kind))
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
