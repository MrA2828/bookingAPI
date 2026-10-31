using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkyWatch.Features.Airports.Dtos;

// Query string of GET /airports/in-view: the rectangle a map is showing.
public sealed class AirportsInViewRequestDto : IValidatableObject
{
    public const int MaxLimit = 2000;
    private static readonly string[] KnownTypes = ["large_airport", "medium_airport", "small_airport"];

    [BindRequired, FromQuery(Name = "south")]
    public double South { get; init; }

    [BindRequired, FromQuery(Name = "north")]
    public double North { get; init; }

    [BindRequired, FromQuery(Name = "west")]
    public double West { get; init; }

    [BindRequired, FromQuery(Name = "east")]
    public double East { get; init; }

    // Repeat the key to pick several: types=large_airport&types=medium_airport. Empty means all three.
    [FromQuery(Name = "types")]
    public string[]? Types { get; init; }

    [FromQuery(Name = "scheduledOnly"), DefaultValue(true)]
    public bool ScheduledOnly { get; init; } = true;

    [FromQuery(Name = "limit"), DefaultValue(1000)]
    public int Limit { get; init; } = 1000;

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
        if (Types is { } types && types.Any(t => !KnownTypes.Contains(t)))
            yield return new ValidationResult($"types must be among {string.Join(", ", KnownTypes)}", ["types"]);
    }
}
