using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyWatch.Entities;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Features.Airports.Queries;
using SkyWatch.Features.ReferenceData.Commands;
using SkyWatch.Features.ReferenceData.Dtos;
using SkyWatch.Features.ReferenceData.Queries;
using SkyWatch.Features.ReferenceData.Sources;

namespace SkyWatch.Controllers;

[ApiController]
[Authorize]
public class ReferenceDataController(ISender sender) : ControllerBase
{
    [HttpGet("airlines")]
    [EndpointSummary("Searches airlines by name or IATA/ICAO code")]
    public async Task<ActionResult<IReadOnlyList<AirlineReturnDto>>> Airlines([FromQuery] SearchAirlinesRequestDto request, CancellationToken ct) =>
        Ok(await sender.Send(new SearchAirlinesQuery(request), ct));

    [HttpGet("countries")]
    [EndpointSummary("Lists all countries (ISO code, name, continent)")]
    public async Task<IActionResult> Countries(CancellationToken ct) => Ok(await sender.Send(new GetCountriesQuery(), ct));

    [HttpGet("reference-data/status")]
    [EndpointSummary("Shows when each reference dataset was last synced and what the last run changed")]
    public async Task<IActionResult> Status(CancellationToken ct) => Ok(await sender.Send(new GetSyncStatusQuery(), ct));

    [HttpPost("reference-data/sync")]
    [Authorize(Roles = Roles.Admin)]
    [EndpointSummary("Forces an immediate re-sync of all reference datasets (admin only)")]
    public async Task<IActionResult> Sync(CancellationToken ct) =>
        Ok(await sender.Send(new SyncReferenceDataCommand(ReferenceSources.All), ct));
}
