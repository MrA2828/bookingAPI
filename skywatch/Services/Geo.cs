namespace SkyWatch.Services;

public static class Geo
{
    private const double EarthRadiusKm = 6371.0088;

    // Great-circle (haversine) distance between two points, in kilometres.
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public const double KilometresPerNauticalMile = 1.852;

    // Points along the great-circle path from one position to another, both ends included.
    public static IReadOnlyList<(double Latitude, double Longitude)> GreatCirclePath(
        double lat1, double lon1, double lat2, double lon2, int segments)
    {
        var phi1 = ToRadians(lat1);
        var lambda1 = ToRadians(lon1);
        var phi2 = ToRadians(lat2);
        var lambda2 = ToRadians(lon2);

        var angle = DistanceKm(lat1, lon1, lat2, lon2) / EarthRadiusKm;
        var points = new List<(double, double)>(segments + 1);
        if (angle < 1e-9)
        {
            points.Add((lat1, lon1));
            return points;
        }

        for (var i = 0; i <= segments; i++)
        {
            var fraction = (double)i / segments;
            var a = Math.Sin((1 - fraction) * angle) / Math.Sin(angle);
            var b = Math.Sin(fraction * angle) / Math.Sin(angle);
            var x = a * Math.Cos(phi1) * Math.Cos(lambda1) + b * Math.Cos(phi2) * Math.Cos(lambda2);
            var y = a * Math.Cos(phi1) * Math.Sin(lambda1) + b * Math.Cos(phi2) * Math.Sin(lambda2);
            var z = a * Math.Sin(phi1) + b * Math.Sin(phi2);
            points.Add((ToDegrees(Math.Atan2(z, Math.Sqrt(x * x + y * y))), ToDegrees(Math.Atan2(y, x))));
        }
        return points;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    private static double ToDegrees(double radians) => radians * 180 / Math.PI;
}
