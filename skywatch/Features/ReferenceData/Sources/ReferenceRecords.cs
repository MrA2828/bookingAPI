namespace SkyWatch.Features.ReferenceData.Sources;

// Parsed, normalised rows from the external datasets. Records give us value equality,
// which the sync uses to detect whether a stored row actually changed.
public record AirportRecord(
    int Id, string Ident, string Type, string Name, double Latitude, double Longitude, int? ElevationFt,
    string Continent, string CountryCode, string RegionCode, string? Municipality, bool ScheduledService,
    string? IcaoCode, string? IataCode);

public record AirlineRecord(
    int Id, string Name, string? Alias, string? IataCode, string? IcaoCode, string? Callsign, string? Country);

public record CountryRecord(string Code, string Name, string Continent);

public static class ReferenceSources
{
    public const string Airports = "airports";
    public const string Airlines = "airlines";
    public const string Countries = "countries";

    public static readonly string[] All = [Countries, Airports, Airlines];
}
