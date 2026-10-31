using MediatR;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Features.Aircraft.Queries;

public record AircraftInViewQuery(AircraftInViewRequestDto Request) : IRequest<AircraftInViewReturnDto>;

public class AircraftInViewHandler(IAircraftFeed feed, IAirlineCodes airlineCodes, IOptions<AircraftOptions> options)
    : IRequestHandler<AircraftInViewQuery, AircraftInViewReturnDto>
{
    // Be gentle with the public sources (adsb.fi asks for about one request a second, adsb.lol answers 429 to bursts):
    // a cold wide view is several calls, but never many at once.
    private const int MaxParallelTiles = 2;

    public async Task<AircraftInViewReturnDto> Handle(AircraftInViewQuery q, CancellationToken ct)
    {
        var r = q.Request;
        var tiles = ViewTiles.Cover(r.South, r.North, r.West, r.East);

        Exception? lastError = null;
        using var gate = new SemaphoreSlim(MaxParallelTiles);
        var results = await Task.WhenAll(tiles.Select(async tile =>
        {
            await gate.WaitAsync(ct);
            try
            {
                return await feed.GetAroundAsync(tile.Latitude, tile.Longitude, ViewTiles.RadiusNauticalMiles, ct,
                    options.Value.WideViewMaxAge);
            }
            catch (AircraftFeedUnavailableException ex)
            {
                lastError = ex;
                return null; // the rest of the view is still worth showing
            }
            finally
            {
                gate.Release();
            }
        }));

        var snapshots = results.OfType<AircraftSnapshot>().ToList();
        if (snapshots.Count == 0)
            throw new AircraftFeedUnavailableException("No live aircraft source is reachable right now", lastError);

        // Neighbouring circles overlap, so the same aircraft can come back more than once.
        var unique = new Dictionary<string, AircraftState>();
        foreach (var aircraft in snapshots.SelectMany(s => s.Aircraft)) unique.TryAdd(aircraft.Hex, aircraft);

        var airlineIcaoCodes = await airlineCodes.GetIcaoCodesAsync(ct);
        var filter = new AircraftFilter(r.Kinds, r.IncludeOnGround, r.MinAltitudeFt, r.MaxAltitudeFt);
        var matches = unique.Values
            .Where(a => a.Latitude >= r.South && a.Latitude <= r.North && a.Longitude >= r.West && a.Longitude <= r.East)
            .Select(a => (State: a, Kind: AircraftClassifier.Classify(a, airlineIcaoCodes)))
            .Where(x => filter.Matches(x.State, x.Kind))
            .OrderByDescending(x => x.State.AltitudeFt ?? int.MinValue)
            .ThenBy(x => x.State.Hex, StringComparer.Ordinal)
            .ToList();

        return new AircraftInViewReturnDto(
            string.Join(", ", snapshots.Select(s => s.Source).Distinct().Order()),
            snapshots.Min(s => s.FetchedAt),
            snapshots.Any(s => s.Stale),
            snapshots.Count == tiles.Count,
            matches.Count,
            matches.Take(r.Limit).Select(x => AircraftReturnDto.From(x.State, x.Kind)).ToList());
    }
}
