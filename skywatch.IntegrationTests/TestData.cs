using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.IntegrationTests;

public static class TestData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Real London airports, so the nearby search has a known right answer.
        db.Airports.AddRange(
            Airport(1, "EGLL", "large_airport", "London Heathrow Airport", 51.4706, -0.4619, "GB", "LHR", "EGLL", scheduled: true, city: "London"),
            Airport(2, "EGKK", "large_airport", "London Gatwick Airport", 51.1537, -0.1821, "GB", "LGW", "EGKK", scheduled: true, city: "London"),
            Airport(3, "EGLC", "medium_airport", "London City Airport", 51.5053, 0.0553, "GB", "LCY", "EGLC", scheduled: true, city: "London"),
            Airport(4, "EGGW", "medium_airport", "London Luton Airport", 51.8747, -0.3683, "GB", "LTN", "EGGW", scheduled: true, city: "Luton"),
            Airport(5, "KJFK", "large_airport", "John F Kennedy International Airport", 40.6398, -73.7789, "US", "JFK", "KJFK", scheduled: true, city: "New York"),
            Airport(6, "EGXX", "small_airport", "Grass Strip", 51.50, -0.40, "GB", null, null, scheduled: false, city: "Hayes"),
            Airport(7, "EGZZ", "large_airport", "Zombie Airport", 51.40, -0.30, "GB", "ZMB", "EGZZ", scheduled: true, city: "Gone", active: false));

        // Enough rows in one country to prove paging and the page-size cap (100).
        for (var i = 0; i < 120; i++)
            db.Airports.Add(Airport(100 + i, $"XX{i:000}", "small_airport", $"Filler Field {i:000}", 10 + i * 0.01, 10, "XX", null, null, scheduled: false));

        db.Airlines.AddRange(
            Airline(1, "British Airways", "BA", "BAW", "SPEEDBIRD", "United Kingdom"),
            Airline(2, "United Airlines", "UA", "UAL", "UNITED", "United States"),
            Airline(3, "Baltic Test Air", "BT", "BTT", null, "Latvia"),
            Airline(4, "Defunct Air", "DF", "DFA", null, "Nowhere", active: false));
        for (var i = 0; i < 25; i++)
            db.Airlines.Add(Airline(100 + i, $"Carrier {i:00}", null, null, null, "Testland"));

        await db.SaveChangesAsync();
    }

    private static Airport Airport(int id, string ident, string type, string name, double latitude, double longitude, string country,
        string? iata, string? icao, bool scheduled, string? city = null, bool active = true) => new()
    {
        Id = id, Ident = ident, Type = type, Name = name, Latitude = latitude, Longitude = longitude, Continent = "EU",
        CountryCode = country, RegionCode = $"{country}-X", Municipality = city, ScheduledService = scheduled,
        IataCode = iata, IcaoCode = icao, IsActive = active, UpdatedAt = DateTime.UtcNow
    };

    private static Airline Airline(int id, string name, string? iata, string? icao, string? callsign, string country, bool active = true) => new()
    {
        Id = id, Name = name, IataCode = iata, IcaoCode = icao, Callsign = callsign, Country = country, IsActive = active, UpdatedAt = DateTime.UtcNow
    };
}
