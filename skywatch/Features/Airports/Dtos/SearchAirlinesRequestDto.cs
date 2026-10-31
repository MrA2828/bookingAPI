using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace SkyWatch.Features.Airports.Dtos;

// Query string of GET /airlines.
public sealed class SearchAirlinesRequestDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    [FromQuery(Name = "pageNumber"), DefaultValue(1)]
    public int PageNumber { get; init; } = 1;

    [FromQuery(Name = "pageSize"), DefaultValue(20)]
    public int PageSize { get; init; } = 20;
}
