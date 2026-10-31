using System.ComponentModel.DataAnnotations;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Airports.Dtos;

namespace SkyWatch.Tests;

public class RequestDtoValidationTests
{
    private static List<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(51.47, -0.46, 100, true)]
    [InlineData(90, 180, 440, true)]
    [InlineData(91, 0, 100, false)]
    [InlineData(0, -181, 100, false)]
    [InlineData(0, 0, 0, false)]
    [InlineData(0, 0, 441, false)]
    public void Live_aircraft_range_is_checked(double lat, double lon, double radiusKm, bool valid) =>
        Assert.Equal(valid, Validate(new LiveAircraftRequestDto { Latitude = lat, Longitude = lon, RadiusKm = radiusKm }).Count == 0);

    [Theory]
    [InlineData(0, 0, 500, true)]
    [InlineData(0, 0, 501, false)]
    [InlineData(-91, 0, 100, false)]
    public void Nearby_airports_range_is_checked(double lat, double lon, double radiusKm, bool valid) =>
        Assert.Equal(valid, Validate(new NearbyAirportsRequestDto { Latitude = lat, Longitude = lon, RadiusKm = radiusKm }).Count == 0);

    [Fact]
    public void Each_bad_field_is_reported_under_its_query_name()
    {
        var results = Validate(new LiveAircraftRequestDto { Latitude = 99, Longitude = 999, RadiusKm = -1 });
        Assert.Equal(["lat", "lon", "radiusKm"], results.SelectMany(r => r.MemberNames));
    }

    [Theory]
    [InlineData("4ca7b5", true)]
    [InlineData("4CA7B5", true)]
    [InlineData("4ca7b", false)]
    [InlineData("zzzzzz", false)]
    public void Hex_must_be_six_hex_digits(string hex, bool valid) =>
        Assert.Equal(valid, Validate(new GetAircraftRequestDto { Hex = hex }).Count == 0);
}
