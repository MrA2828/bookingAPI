using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkyWatch.Features.Airports.Dtos;

// Query string of GET /airports/nearby.
public sealed class NearbyAirportsRequestDto : IValidatableObject
{
    public const double MaxRadiusKm = 500;

    [BindRequired, FromQuery(Name = "lat")]
    public double Latitude { get; init; }

    [BindRequired, FromQuery(Name = "lon")]
    public double Longitude { get; init; }

    [FromQuery(Name = "radiusKm"), DefaultValue(100.0)]
    public double RadiusKm { get; init; } = 100;

    [FromQuery(Name = "pageNumber"), DefaultValue(1)]
    public int PageNumber { get; init; } = 1;

    [FromQuery(Name = "pageSize"), DefaultValue(20)]
    public int PageSize { get; init; } = 20;

    [FromQuery(Name = "scheduledOnly"), DefaultValue(true)]
    public bool ScheduledOnly { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude is < -90 or > 90)
            yield return new ValidationResult("lat must be -90..90", ["lat"]);
        if (Longitude is < -180 or > 180)
            yield return new ValidationResult("lon must be -180..180", ["lon"]);
        if (RadiusKm is <= 0 or > MaxRadiusKm)
            yield return new ValidationResult($"radiusKm must be greater than 0 and at most {MaxRadiusKm}", ["radiusKm"]);
    }
}
