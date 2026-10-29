namespace SkyWatch.Features.Aircraft.Models;

// Best-effort classification. ADS-B carries no "private or commercial" field, so Commercial / Private are
// heuristics (see AircraftClassifier); Military comes from the source's aircraft database flag.
public enum AircraftKind
{
    Commercial,
    Private,
    Military,
    Unknown
}
