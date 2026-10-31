using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Models;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public static class TestAircraft
{
    public static AircraftState Make(string hex = "abc123", string? callsign = "BAW123", string? registration = "G-ABCD",
        double latitude = 51.5, double longitude = -0.1, int? altitudeFt = 35000, bool onGround = false,
        bool military = false, string? category = null) =>
        new(hex, callsign, registration, "A320", category, latitude, longitude, altitudeFt, 450, 90, 0, onGround, "1234", military);
}

public class FakeAircraftSource(string name, Func<IReadOnlyList<AircraftState>> respond) : IAircraftSource
{
    public int Calls { get; private set; }
    public string Name => name;

    public Task<IReadOnlyList<AircraftState>> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(respond());
    }

    public Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(respond().FirstOrDefault(a => a.Hex == hex));
    }
}

public class FakeAircraftFeed(IReadOnlyList<AircraftState> aircraft, bool stale = false) : IAircraftFeed
{
    public Task<AircraftSnapshot> GetAroundAsync(double latitude, double longitude, int radiusNauticalMiles, CancellationToken ct) =>
        Task.FromResult(new AircraftSnapshot(aircraft, DateTime.UtcNow, "fake", stale));

    public Task<AircraftState?> GetByHexAsync(string hex, CancellationToken ct) =>
        Task.FromResult(aircraft.FirstOrDefault(a => a.Hex == hex));
}

public class FakeAirlineCodes(params string[] codes) : IAirlineCodes
{
    public Task<IReadOnlySet<string>> GetIcaoCodesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlySet<string>>(codes.ToHashSet(StringComparer.OrdinalIgnoreCase));
}
