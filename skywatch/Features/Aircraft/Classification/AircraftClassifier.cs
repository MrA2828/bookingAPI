using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Classification;

// ADS-B has no "commercial or private" field, so this is a heuristic:
//   Military   - the source's aircraft database marks the airframe as military.
//   Commercial - the callsign is an airline's ICAO code plus a flight number (BAW123, UAL9A).
//   Private    - the callsign is just the registration (N123AB), or the airframe is a light aircraft,
//                rotorcraft, glider or balloon (ADS-B emitter category A1, A7, B1-B4, B6, B7).
//   Unknown    - everything else.
public static class AircraftClassifier
{
    private static readonly HashSet<string> PrivateCategories = ["A1", "A7", "B1", "B2", "B3", "B4", "B6", "B7"];

    public static AircraftKind Classify(AircraftState aircraft, IReadOnlySet<string> airlineIcaoCodes)
    {
        if (aircraft.IsMilitary) return AircraftKind.Military;

        var callsign = aircraft.Callsign?.ToUpperInvariant();
        if (callsign is { Length: >= 4 } && char.IsAsciiDigit(callsign[3]) && airlineIcaoCodes.Contains(callsign[..3]))
            return AircraftKind.Commercial;

        if (callsign is not null && aircraft.Registration is { } registration
            && callsign == registration.Replace("-", "").ToUpperInvariant())
            return AircraftKind.Private;

        if (aircraft.Category is { } category && PrivateCategories.Contains(category))
            return AircraftKind.Private;

        return AircraftKind.Unknown;
    }
}
