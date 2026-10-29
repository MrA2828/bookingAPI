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
    public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] string? country, [FromQuery] string? type,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await sender.Send(new SearchAirportsQuery(search, country, type, pageNumber, pageSize), ct));

    [HttpGet("nearby")]
    [EndpointSummary("Lists airports within a radius of a point, nearest first")]
    [EndpointDescription("Give `lat`, `lon` and `radiusKm` (max 500). By default only airports with scheduled service are returned; pass `scheduledOnly=false` to include small airfields. Paged with `pageNumber` (starts at 1) and `pageSize` (max 100).")]
    public async Task<IActionResult> Nearby([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radiusKm = 100,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] bool scheduledOnly = true, CancellationToken ct = default)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180 || radiusKm is <= 0 or > 500)
            return BadRequest("lat must be -90..90, lon -180..180 and radiusKm 0..500");
        return Ok(await sender.Send(new NearbyAirportsQuery(lat, lon, radiusKm, pageNumber, pageSize, scheduledOnly), ct));
    }

    [HttpGet("{code}")]
    [EndpointSummary("Gets one airport by IATA (JFK), ICAO (KJFK) or ident code")]
    public async Task<IActionResult> Get(string code, CancellationToken ct) =>
        await sender.Send(new GetAirportQuery(code), ct) is { } airport ? Ok(airport) : NotFound();
}
