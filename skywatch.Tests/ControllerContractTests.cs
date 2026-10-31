using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SkyWatch.Controllers;

namespace SkyWatch.Tests;

// A missing lat/lon used to bind to 0,0 (open ocean) and return an empty list instead of an error.
public class ControllerContractTests
{
    [Theory]
    [InlineData(typeof(AircraftController), "Live")]
    [InlineData(typeof(AirportsController), "Nearby")]
    public void Latitude_and_longitude_are_required(Type controller, string action)
    {
        var parameters = controller.GetMethod(action)!.GetParameters();

        foreach (var name in new[] { "lat", "lon" })
            Assert.NotNull(parameters.Single(p => p.Name == name).GetCustomAttribute<BindRequiredAttribute>());
    }
}
