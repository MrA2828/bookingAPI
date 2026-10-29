namespace SkyWatch.Features.ReferenceData.Sources;

// Parsed, normalised rows from the external datasets. Records give us value equality,
// which the sync uses to detect whether a stored row actually changed.
public record AirportRecord(
    int Id, string Ident, string Type, string Name, double Latitude, double Longitude, int? ElevationFt,
    string Continent, string CountryCode, string RegionCode, string? Municipality, bool ScheduledService,
    string? IcaoCode, string? IataCode);
