using SkyWatch.Services;

namespace SkyWatch.Tests;

public class GeoTests
{
    [Fact]
    public void Distance_between_London_and_Paris_is_about_344_km()
    {
        var km = Geo.DistanceKm(51.5074, -0.1278, 48.8566, 2.3522);
        Assert.InRange(km, 340, 348);
    }

    [Fact]
    public void Distance_to_the_same_point_is_zero() =>
        Assert.Equal(0, Geo.DistanceKm(10, 20, 10, 20), 6);

    [Fact]
    public void Great_circle_path_starts_and_ends_at_the_given_points()
    {
        var path = Geo.GreatCirclePath(51.47, -0.46, 41.98, -87.90, 48);

        Assert.Equal(49, path.Count);
        Assert.Equal(51.47, path[0].Latitude, 3);
        Assert.Equal(-0.46, path[0].Longitude, 3);
        Assert.Equal(41.98, path[^1].Latitude, 3);
        Assert.Equal(-87.90, path[^1].Longitude, 3);
    }

    [Fact]
    public void Great_circle_path_between_identical_points_does_not_divide_by_zero()
    {
        var path = Geo.GreatCirclePath(10, 10, 10, 10, 48);
        Assert.Single(path);
    }
}
