using SkyWatch.Features.Aircraft.Sources;
using SkyWatch.Services;

namespace SkyWatch.Tests;

public class ViewTilesTests
{
    // Every point of the rectangle must be inside the circle of at least one tile.
    private static void AssertCovered(double south, double north, double west, double east)
    {
        var tiles = ViewTiles.Cover(south, north, west, east);
        var radiusKm = ViewTiles.RadiusNauticalMiles * Geo.KilometresPerNauticalMile;

        for (var lat = south; lat <= north; lat += (north - south) / 40)
            for (var lon = west; lon <= east; lon += (east - west) / 40)
                Assert.True(tiles.Any(t => Geo.DistanceKm(t.Latitude, t.Longitude, lat, lon) <= radiusKm),
                    $"({lat:0.##}, {lon:0.##}) is not covered in view {south}..{north}, {west}..{east}");
    }

    [Theory]
    [InlineData(51.0, 52.0, -1.0, 1.0)]       // a city
    [InlineData(40.0, 60.0, -10.0, 25.0)]     // western Europe
    [InlineData(30.0, 46.0, -125.0, -100.0)]  // the western US
    [InlineData(-10.0, 10.0, 90.0, 115.0)]    // across the equator
    [InlineData(-45.0, -30.0, 140.0, 155.0)]  // south-eastern Australia
    [InlineData(60.0, 72.0, 5.0, 40.0)]       // northern Scandinavia
    [InlineData(-90.0, -75.0, -30.0, 30.0)]   // near the south pole
    public void Every_point_of_the_view_is_inside_a_tile(double south, double north, double west, double east) =>
        AssertCovered(south, north, west, east);

    [Fact]
    public void A_small_view_needs_few_circles()
    {
        var tiles = ViewTiles.Cover(51.0, 52.0, -1.0, 1.0);
        Assert.InRange(tiles.Count, 1, 4);
    }

    [Fact]
    public void A_continent_sized_view_is_over_the_cap_and_the_whole_world_far_over()
    {
        Assert.True(ViewTiles.Cover(-60, 70, -170, 170).Count > ViewTiles.MaxTiles);
        Assert.True(ViewTiles.Cover(35, 60, -20, 50).Count > ViewTiles.MaxTiles);
    }

    [Fact]
    public void A_view_about_1500_km_across_fits_in_the_cap()
    {
        Assert.InRange(ViewTiles.Cover(45, 58, -10, 12).Count, 1, ViewTiles.MaxTiles);
    }

    [Fact]
    public void Tile_centres_sit_on_a_fixed_grid_so_overlapping_views_share_circles()
    {
        var a = ViewTiles.Cover(45, 55, -5, 10).ToHashSet();
        var b = ViewTiles.Cover(47, 57, 0, 15).ToHashSet();
        Assert.NotEmpty(a.Intersect(b));
    }

    [Fact]
    public void Tile_centres_are_valid_coordinates_and_unique()
    {
        var tiles = ViewTiles.Cover(-90, 90, -180, 180);
        Assert.All(tiles, t =>
        {
            Assert.InRange(t.Latitude, -90, 90);
            Assert.InRange(t.Longitude, -180, 180);
        });
        Assert.Equal(tiles.Count, tiles.Distinct().Count());
    }
}
