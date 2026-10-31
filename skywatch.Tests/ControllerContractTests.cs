using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SkyWatch.Features.Aircraft.Dtos;
using SkyWatch.Features.Airports.Dtos;

namespace SkyWatch.Tests;

// A missing lat/lon used to bind to 0,0 (open ocean) and return an empty list instead of an error.
public class ControllerContractTests
{
    [Theory]
    [InlineData(typeof(LiveAircraftRequestDto))]
    [InlineData(typeof(NearbyAirportsRequestDto))]
    public void Latitude_and_longitude_are_required(Type requestDto)
    {
        foreach (var name in new[] { "Latitude", "Longitude" })
            Assert.NotNull(requestDto.GetProperty(name)!.GetCustomAttribute<BindRequiredAttribute>());
    }
}
