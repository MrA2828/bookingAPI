using SkyWatch.Services;

namespace SkyWatch.Features.Aircraft.Sources;

// The sources only answer "aircraft within N nm of a point" (250 nm at most), so a map rectangle bigger than
// one circle is covered by several. Circle centres sit on a fixed global grid, so two views that overlap ask for
// the same circles and share the cached answers. Rows are RowStepDegrees apart; within a row the columns are
// as far apart as the circle still reaches across.
public static class ViewTiles
{
    // Asked of the feed, which widens it by its snap margin and caps it at the sources' 250 nm.
    public const int RadiusNauticalMiles = 232;

    // A view needing more circles than this is too big to serve in one call.
    public const int MaxTiles = 20;

    private const double RowStepDegrees = 5;
    private const double KilometresPerDegree = 111.195;

    public static IReadOnlyList<(double Latitude, double Longitude)> Cover(double south, double north, double west, double east)
    {
        var radiusKm = RadiusNauticalMiles * Geo.KilometresPerNauticalMile;
        var halfRowKm = RowStepDegrees / 2 * KilometresPerDegree;
        // How far sideways from its centre the circle still reaches at the top and bottom edge of its row.
        var halfColumnKm = Math.Sqrt(radiusKm * radiusKm - halfRowKm * halfRowKm);

        var tiles = new List<(double, double)>();
        for (var row = (int)Math.Floor(south / RowStepDegrees) - 1; row <= (int)Math.Ceiling(north / RowStepDegrees) + 1; row++)
        {
            var latitude = row * RowStepDegrees;
            var bandSouth = Math.Max(latitude - RowStepDegrees / 2, -90);
            var bandNorth = Math.Min(latitude + RowStepDegrees / 2, 90);
            if (Math.Abs(latitude) > 90 || bandNorth <= south || bandSouth >= north) continue;

            // Degrees of longitude cover the most ground at the band's edge nearest the equator.
            var nearestToEquator = bandSouth <= 0 && bandNorth >= 0 ? 0 : Math.Min(Math.Abs(bandSouth), Math.Abs(bandNorth));
            var columnStep = (int)Math.Clamp(Math.Floor(2 * halfColumnKm / (KilometresPerDegree * Math.Cos(nearestToEquator * Math.PI / 180))), 1, 360);

            for (var column = (int)Math.Floor(west / columnStep); column <= (int)Math.Ceiling(east / columnStep); column++)
            {
                var longitude = column * columnStep;
                if (longitude + columnStep / 2.0 <= west || longitude - columnStep / 2.0 >= east) continue;
                tiles.Add((latitude, Wrap(longitude)));
            }
        }
        return tiles;
    }

    private static double Wrap(double longitude) => longitude > 180 ? longitude - 360 : longitude < -180 ? longitude + 360 : longitude;
}
