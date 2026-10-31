using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Features.Airports.Queries;

namespace SkyWatch.Controllers;

[ApiController]
[Authorize]
[Route("airports")]
public class AirportsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Searches airports by name, city or IATA/ICAO code")]
    [EndpointDescription("`search` matches an exact IATA/ICAO/ident code first, then names and cities (case-insensitive). Optional filters: `country` (ISO code, e.g. `GB`) and `type` (`large_airport`, `medium_airport`, `small_airport`). Paged with `pageNumber` (starts at 1) and `pageSize` (max 100).")]
    public async Task<ActionResult<IReadOnlyList<AirportReturnDto>>> Search([FromQuery] SearchAirportsRequestDto request, CancellationToken ct) =>
        Ok(await sender.Send(new SearchAirportsQuery(request), ct));

    [HttpGet("nearby")]
    [EndpointSummary("Lists airports within a radius of a point, nearest first")]
    [EndpointDescription("Give `lat`, `lon` and `radiusKm` (max 500). By default only airports with scheduled service are returned; pass `scheduledOnly=false` to include small airfields. Paged with `pageNumber` (starts at 1) and `pageSize` (max 100).")]
    public async Task<ActionResult<IReadOnlyList<NearbyAirportReturnDto>>> Nearby([FromQuery] NearbyAirportsRequestDto request, CancellationToken ct) =>
        Ok(await sender.Send(new NearbyAirportsQuery(request), ct));

    [HttpGet("in-view")]
    [EndpointSummary("Lists the airports inside a map rectangle, biggest first")]
    [EndpointDescription("Give `south`, `north`, `west` and `east` (a view that crosses the antimeridian must be split into two calls). `types` (repeat it: `types=large_airport&types=medium_airport`) picks airport sizes, default all. By default only airports with scheduled service are returned; pass `scheduledOnly=false` to include small airfields. At most `limit` airports (default 1000, max 2000) are returned, large ones first, so zooming in shows the rest.")]
    public async Task<ActionResult<IReadOnlyList<AirportReturnDto>>> InView([FromQuery] AirportsInViewRequestDto request, CancellationToken ct) =>
        Ok(await sender.Send(new AirportsInViewQuery(request), ct));

    [HttpGet("{code}")]
    [EndpointSummary("Gets one airport by IATA (JFK), ICAO (KJFK) or ident code")]
    public async Task<ActionResult<AirportReturnDto>> Get(string code, CancellationToken ct) =>
        await sender.Send(new GetAirportQuery(code), ct) is { } airport ? Ok(airport) : NotFound();
}
