using System.Text;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class AdsbV2ParserTests
{
    private static Stream Json(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void Reads_the_adsb_lol_shape()
    {
        var aircraft = AdsbV2Parser.Parse(Json("""
            {"ac":[{"hex":"406F72","flight":"BAW7CP  ","r":"G-ZBKF","t":"B789","alt_baro":35000,"gs":450.4,"track":89.6,
                    "baro_rate":-64,"squawk":"4764","category":"A5","lat":51.46,"lon":-0.46}]}
            """));

        var a = Assert.Single(aircraft);
        Assert.Equal("406f72", a.Hex);
        Assert.Equal("BAW7CP", a.Callsign);
        Assert.Equal("G-ZBKF", a.Registration);
        Assert.Equal(35000, a.AltitudeFt);
        Assert.Equal(450, a.GroundSpeedKt);
        Assert.Equal(-64, a.VerticalRateFpm);
        Assert.False(a.OnGround);
        Assert.False(a.IsMilitary);
    }

    [Fact]
    public void Reads_the_adsb_fi_shape()
    {
        var aircraft = AdsbV2Parser.Parse(Json("""{"aircraft":[{"hex":"407d9a","lat":51.3,"lon":-0.8,"alt_baro":275}]}"""));
        Assert.Single(aircraft);
    }

    [Fact]
    public void Treats_alt_baro_ground_as_on_the_ground_with_no_altitude()
    {
        var a = Assert.Single(AdsbV2Parser.Parse(Json("""{"ac":[{"hex":"aaaaaa","lat":1,"lon":2,"alt_baro":"ground"}]}""")));
        Assert.True(a.OnGround);
        Assert.Null(a.AltitudeFt);
    }

    [Fact]
    public void Skips_aircraft_without_a_position()
    {
        var aircraft = AdsbV2Parser.Parse(Json("""{"ac":[{"hex":"aaaaaa"},{"hex":"bbbbbb","lat":1,"lon":2}]}"""));
        Assert.Equal("bbbbbb", Assert.Single(aircraft).Hex);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]   // military + interesting
    [InlineData(2, false)]  // interesting only
    [InlineData(0, false)]
    public void Reads_the_military_flag_from_the_first_bit_of_dbFlags(int flags, bool military)
    {
        var a = Assert.Single(AdsbV2Parser.Parse(Json($$"""{"ac":[{"hex":"aaaaaa","lat":1,"lon":2,"dbFlags":{{flags}}}]}""")));
        Assert.Equal(military, a.IsMilitary);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"ac":[]}""")]
    [InlineData("""{"ac":"nope"}""")]
    public void Returns_nothing_for_empty_or_unexpected_bodies(string body) =>
        Assert.Empty(AdsbV2Parser.Parse(Json(body)));
}
