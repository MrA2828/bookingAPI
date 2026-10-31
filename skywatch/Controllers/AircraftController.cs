using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Queries;

namespace SkyWatch.Controllers;

[ApiController]
[Authorize]
[Route("aircraft")]
public partial class AircraftController(ISender sender) : ControllerBase
{
    private const double MaxRadiusKm = 440;

    [GeneratedRegex("^[0-9a-fA-F]{6}$")]
    private static partial Regex HexPattern();

    [HttpGet]
    [EndpointSummary("Lists live aircraft around a point, nearest first")]
    [EndpointDescription("Give `lat`, `lon` and `radiusKm` (max 440). Filters: `kinds` (repeat it: `kinds=commercial&kinds=private`; values `commercial`, `private`, `military`, `unknown`; by default only commercial and unknown aircraft are returned, so military and private are excluded), `minAltitudeFt`, `maxAltitudeFt`, and `includeOnGround` (default false). `kind` is a best-effort guess: ADS-B has no private/commercial field, so it is derived from the callsign, registration and aircraft category; military comes from an aircraft database flag. Positions are cached for about 10 seconds; `source`, `fetchedAt` and `stale` say where and when they were read. Paged with `pageNumber` (starts at 1) and `pageSize` (max 100).")]
    public async Task<IActionResult> Live([BindRequired, FromQuery] double lat, [BindRequired, FromQuery] double lon, [FromQuery] double radiusKm = 100,
        [FromQuery] AircraftKind[]? kinds = null, [FromQuery] int? minAltitudeFt = null, [FromQuery] int? maxAltitudeFt = null,
        [FromQuery] bool includeOnGround = false, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180 || radiusKm is <= 0 or > MaxRadiusKm)
            return BadRequest($"lat must be -90..90, lon -180..180 and radiusKm 0..{MaxRadiusKm}");
        return Ok(await sender.Send(new GetLiveAircraftQuery(
            lat, lon, radiusKm, kinds, minAltitudeFt, maxAltitudeFt, includeOnGround, pageNumber, pageSize), ct));
    }

    [HttpGet("{hex}")]
    [EndpointSummary("Gets one aircraft by its ICAO 24-bit hex address, with airline, route, planned path and estimated arrival")]
    [EndpointDescription("`hex` is the six-character transponder address (e.g. `4ca7b5`) from the list endpoint. The route (origin, destination, great-circle path, distance and estimated arrival) is looked up from the callsign and is null when the callsign is not in the route database. 404 means the aircraft is not currently being tracked.")]
    public async Task<IActionResult> Get(string hex, CancellationToken ct)
    {
        if (!HexPattern().IsMatch(hex)) return BadRequest("hex must be six hexadecimal characters");
        return await sender.Send(new GetAircraftQuery(hex), ct) is { } aircraft ? Ok(aircraft) : NotFound();
    }
}
