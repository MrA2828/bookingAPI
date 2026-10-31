using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Tests;

public class AircraftClassifierTests
{
    private static readonly IReadOnlySet<string> Airlines = new HashSet<string> { "BAW", "UAL", "FDX" };

    [Fact]
    public void Military_flag_wins_over_an_airline_callsign() =>
        Assert.Equal(AircraftKind.Military, AircraftClassifier.Classify(TestAircraft.Make(callsign: "BAW123", military: true), Airlines));

    [Theory]
    [InlineData("BAW123")]
    [InlineData("baw7cp")]
    [InlineData("UAL9A")]
    [InlineData("FDX1234")]
    public void Airline_code_plus_flight_number_is_commercial(string callsign) =>
        Assert.Equal(AircraftKind.Commercial, AircraftClassifier.Classify(TestAircraft.Make(callsign: callsign), Airlines));

    [Fact]
    public void Callsign_equal_to_registration_is_private() =>
        Assert.Equal(AircraftKind.Private, AircraftClassifier.Classify(TestAircraft.Make(callsign: "N123AB", registration: "N123AB"), Airlines));

    [Fact]
    public void Hyphens_in_the_registration_are_ignored() =>
        Assert.Equal(AircraftKind.Private, AircraftClassifier.Classify(TestAircraft.Make(callsign: "GCMFF", registration: "G-CMFF"), Airlines));

    [Theory]
    [InlineData("A1")] // light
    [InlineData("A7")] // rotorcraft
    [InlineData("B1")] // glider
    public void Light_aircraft_categories_are_private(string category) =>
        Assert.Equal(AircraftKind.Private, AircraftClassifier.Classify(TestAircraft.Make(callsign: "XYZ", registration: "G-AAAA", category: category), Airlines));

    [Fact]
    public void Three_letters_that_are_not_an_airline_are_unknown() =>
        Assert.Equal(AircraftKind.Unknown, AircraftClassifier.Classify(TestAircraft.Make(callsign: "ZZZ123", registration: "G-AAAA"), Airlines));

    [Fact]
    public void No_callsign_and_no_clues_is_unknown() =>
        Assert.Equal(AircraftKind.Unknown, AircraftClassifier.Classify(TestAircraft.Make(callsign: null, registration: null), Airlines));

    [Fact]
    public void A_short_callsign_does_not_crash() =>
        Assert.Equal(AircraftKind.Unknown, AircraftClassifier.Classify(TestAircraft.Make(callsign: "BA", registration: null), Airlines));
}
