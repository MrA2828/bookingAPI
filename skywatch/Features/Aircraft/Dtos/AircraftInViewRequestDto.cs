using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Features.Aircraft.Dtos;

// Query string of GET /aircraft/in-view: the rectangle a map is showing.
public sealed class AircraftInViewRequestDto : IValidatableObject
{
    public const int MaxLimit = 5000;

    [BindRequired, FromQuery(Name = "south")]
    public double South { get; init; }

    [BindRequired, FromQuery(Name = "north")]
    public double North { get; init; }

    [BindRequired, FromQuery(Name = "west")]
    public double West { get; init; }

    [BindRequired, FromQuery(Name = "east")]
    public double East { get; init; }

    // Repeat the key to pick several: kinds=commercial&kinds=military.
    [FromQuery(Name = "kinds")]
    public AircraftKind[]? Kinds { get; init; }

    [FromQuery(Name = "minAltitudeFt")]
    public int? MinAltitudeFt { get; init; }

    [FromQuery(Name = "maxAltitudeFt")]
    public int? MaxAltitudeFt { get; init; }

    [FromQuery(Name = "includeOnGround"), DefaultValue(false)]
    public bool IncludeOnGround { get; init; }

    [FromQuery(Name = "limit"), DefaultValue(1500)]
    public int Limit { get; init; } = 1500;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (South is < -90 or > 90 || North is < -90 or > 90)
            yield return new ValidationResult("south and north must be -90..90", ["south", "north"]);
        if (South >= North)
            yield return new ValidationResult("south must be less than north", ["south"]);
        if (West is < -180 or > 180 || East is < -180 or > 180)
            yield return new ValidationResult("west and east must be -180..180", ["west", "east"]);
        if (West >= East)
            yield return new ValidationResult("west must be less than east (split a view that crosses the antimeridian into two calls)", ["west"]);
        if (Limit is < 1 or > MaxLimit)
            yield return new ValidationResult($"limit must be 1..{MaxLimit}", ["limit"]);

        // Only count the circles once the corners are sane, so a bad request is reported for what it is.
        if (South < North && West < East && South >= -90 && North <= 90 && West >= -180 && East <= 180
            && ViewTiles.Cover(South, North, West, East).Count > ViewTiles.MaxTiles)
            yield return new ValidationResult("the view is too large; zoom in (about 1,500 km across at mid latitudes is the most)", ["south", "north", "west", "east"]);
    }
}
