using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Aircraft.Queries;

namespace SkyWatch.Controllers;

[ApiController]
[Authorize]
[Route("aircraft")]
public class AircraftController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists live aircraft around a point, nearest first")]
    [EndpointDescription("Give `lat`, `lon` and `radiusKm` (max 440). Filters: `kinds` (repeat it: `kinds=commercial&kinds=private`; values `commercial`, `private`, `military`, `unknown`; by default only commercial and unknown aircraft are returned, so military and private are excluded), `minAltitudeFt`, `maxAltitudeFt`, and `includeOnGround` (default false). `kind` is a best-effort guess: ADS-B has no private/commercial field, so it is derived from the callsign, registration and aircraft category; military comes from an aircraft database flag. Positions are cached for about 10 seconds; `source`, `fetchedAt` and `stale` say where and when they were read. Paged with `pageNumber` (starts at 1) and `pageSize` (max 100).")]
    public async Task<ActionResult<LiveAircraftPageReturnDto>> Live([FromQuery] LiveAircraftRequestDto request, CancellationToken ct) =>
        Ok(await sender.Send(new GetLiveAircraftQuery(request), ct));

    [HttpGet("{hex}")]
    [EndpointSummary("Gets one aircraft by its ICAO 24-bit hex address, with airline, route, planned path and estimated arrival")]
    [EndpointDescription("`hex` is the six-character transponder address (e.g. `4ca7b5`) from the list endpoint. The route (origin, destination, great-circle path, distance and estimated arrival) is looked up from the callsign and is null when the callsign is not in the route database. 404 means the aircraft is not currently being tracked.")]
    public async Task<ActionResult<AircraftDetailReturnDto>> Get([FromRoute] GetAircraftRequestDto request, CancellationToken ct) =>
        await sender.Send(new GetAircraftQuery(request), ct) is { } aircraft ? Ok(aircraft) : NotFound();
}
