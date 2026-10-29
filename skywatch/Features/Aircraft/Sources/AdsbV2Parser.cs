using System.Text.Json;
using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

// adsb.lol and adsb.fi both serve the readsb "v2" JSON: an array of aircraft under "ac" (adsb.lol) or
// "aircraft" (adsb.fi). Fields are optional and alt_baro is the string "ground" for aircraft on the ground.
public static class AdsbV2Parser
{
    public static IReadOnlyList<AircraftState> Parse(Stream json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!(root.TryGetProperty("ac", out var array) || root.TryGetProperty("aircraft", out array))
            || array.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<AircraftState>(array.GetArrayLength());
        foreach (var item in array.EnumerateArray())
        {
            var hex = Text(item, "hex");
            if (hex is null || !TryNumber(item, "lat", out var latitude) || !TryNumber(item, "lon", out var longitude))
                continue; // no position, nothing to show

            var onGround = false;
            int? altitudeFt = null;
            if (item.TryGetProperty("alt_baro", out var altitude))
            {
                if (altitude.ValueKind == JsonValueKind.String && altitude.GetString() == "ground") onGround = true;
                else if (altitude.ValueKind == JsonValueKind.Number) altitudeFt = (int)Math.Round(altitude.GetDouble());
            }

            double? verticalRate = TryNumber(item, "baro_rate", out var baro) ? baro
                : TryNumber(item, "geom_rate", out var geom) ? geom : null;

            result.Add(new AircraftState(
                Hex: hex.ToLowerInvariant(),
                Callsign: Text(item, "flight"),
                Registration: Text(item, "r"),
                TypeCode: Text(item, "t"),
                Category: Text(item, "category"),
                Latitude: latitude,
                Longitude: longitude,
                AltitudeFt: altitudeFt,
                GroundSpeedKt: TryNumber(item, "gs", out var speed) ? (int)Math.Round(speed) : null,
                TrackDegrees: TryNumber(item, "track", out var track) ? track : null,
                VerticalRateFpm: verticalRate is { } rate ? (int)Math.Round(rate) : null,
                OnGround: onGround,
                Squawk: Text(item, "squawk"),
                IsMilitary: TryNumber(item, "dbFlags", out var flags) && ((int)flags & 1) == 1));
        }
        return result;
    }

    private static string? Text(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String) return null;
        var text = value.GetString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static bool TryNumber(JsonElement item, string name, out double number)
    {
        number = 0;
        return item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out number);
    }
}
