using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace SkyWatch.Features.Airports.Dtos;

// Query string of GET /airports.
public sealed class SearchAirportsRequestDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    [FromQuery(Name = "country")]
    public string? Country { get; init; }

    [FromQuery(Name = "type")]
    public string? Type { get; init; }

    [FromQuery(Name = "pageNumber"), DefaultValue(1)]
    public int PageNumber { get; init; } = 1;

    [FromQuery(Name = "pageSize"), DefaultValue(20)]
    public int PageSize { get; init; } = 20;
}
