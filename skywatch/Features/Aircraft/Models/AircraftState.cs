namespace SkyWatch.Features.Aircraft.Models;

// One aircraft as reported by an ADS-B source at one moment. Altitude in feet, speed in knots,
// vertical rate in feet per minute; null when the aircraft did not report the value.
public record AircraftState(
    string Hex,
    string? Callsign,
    string? Registration,
    string? TypeCode,
    string? Category,
    double Latitude,
    double Longitude,
    int? AltitudeFt,
    int? GroundSpeedKt,
    double? TrackDegrees,
    int? VerticalRateFpm,
    bool OnGround,
    string? Squawk,
    bool IsMilitary);
