using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Classification;

// The filters shared by the list endpoints. Military and private aircraft are left out unless asked for.
public sealed class AircraftFilter(AircraftKind[]? kinds, bool includeOnGround, int? minAltitudeFt, int? maxAltitudeFt)
{
    private static readonly AircraftKind[] DefaultKinds = [AircraftKind.Commercial, AircraftKind.Unknown];

    private readonly HashSet<AircraftKind> _kinds = (kinds is { Length: > 0 } ? kinds : DefaultKinds).ToHashSet();

    public bool Matches(AircraftState aircraft, AircraftKind kind) =>
        _kinds.Contains(kind)
        && (includeOnGround || !aircraft.OnGround)
        && (minAltitudeFt is not { } min || aircraft.AltitudeFt >= min)
        && (maxAltitudeFt is not { } max || aircraft.AltitudeFt <= max);
}
