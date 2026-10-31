using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Dtos;

// Altitude in feet, speed in knots, vertical rate in feet per minute. DistanceKm is from the point that was
// searched around (null on the single-aircraft endpoint).
public record AircraftReturnDto(string Hex, string? Callsign, string? Registration, string? TypeCode, AircraftKind Kind,
    double Latitude, double Longitude, int? AltitudeFt, int? GroundSpeedKt, double? TrackDegrees, int? VerticalRateFpm,
    bool OnGround, string? Squawk, double? DistanceKm)
{
    public static AircraftReturnDto From(AircraftState a, AircraftKind kind, double? distanceKm = null) => new(
        a.Hex, a.Callsign, a.Registration, a.TypeCode, kind, a.Latitude, a.Longitude, a.AltitudeFt, a.GroundSpeedKt,
        a.TrackDegrees, a.VerticalRateFpm, a.OnGround, a.Squawk, distanceKm is { } d ? Math.Round(d, 1) : null);
}
