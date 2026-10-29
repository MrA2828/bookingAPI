using SkyWatch.Features.ReferenceData.Sources;

namespace SkyWatch.Entities;

// Reference rows imported from an external dataset. Rows that vanish from the source are
// flagged IsActive = false instead of being deleted, so history and references stay intact.
public interface ISyncedEntity
{
    int Id { get; }
    bool IsActive { get; set; }
    DateTime UpdatedAt { get; set; }
}

public class Airport : ISyncedEntity
{
    public int Id { get; set; }                     // OurAirports id
    public string Ident { get; set; } = "";         // OurAirports ident (ICAO code or local code)
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? ElevationFt { get; set; }
    public string Continent { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string RegionCode { get; set; } = "";
    public string? Municipality { get; set; }
    public bool ScheduledService { get; set; }
    public string? IcaoCode { get; set; }
    public string? IataCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }

    public static Airport From(AirportRecord r)
    {
        var airport = new Airport { Id = r.Id };
        airport.Apply(r);
        return airport;
    }

    public AirportRecord ToRecord() => new(Id, Ident, Type, Name, Latitude, Longitude, ElevationFt, Continent,
        CountryCode, RegionCode, Municipality, ScheduledService, IcaoCode, IataCode);

    public void Apply(AirportRecord r)
    {
        Ident = r.Ident;
        Type = r.Type;
        Name = r.Name;
        Latitude = r.Latitude;
        Longitude = r.Longitude;
        ElevationFt = r.ElevationFt;
        Continent = r.Continent;
        CountryCode = r.CountryCode;
        RegionCode = r.RegionCode;
        Municipality = r.Municipality;
        ScheduledService = r.ScheduledService;
        IcaoCode = r.IcaoCode;
        IataCode = r.IataCode;
    }
}

public class Airline : ISyncedEntity
{
    public int Id { get; set; }                     // OpenFlights id
    public string Name { get; set; } = "";
    public string? Alias { get; set; }
    public string? IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public string? Callsign { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }

    public static Airline From(AirlineRecord r)
    {
        var airline = new Airline { Id = r.Id };
        airline.Apply(r);
        return airline;
    }

    public AirlineRecord ToRecord() => new(Id, Name, Alias, IataCode, IcaoCode, Callsign, Country);

    public void Apply(AirlineRecord r)
    {
        Name = r.Name;
        Alias = r.Alias;
        IataCode = r.IataCode;
        IcaoCode = r.IcaoCode;
        Callsign = r.Callsign;
        Country = r.Country;
    }
}

public class Country
{
    public string Code { get; set; } = "";          // ISO 3166-1 alpha-2
    public string Name { get; set; } = "";
    public string Continent { get; set; } = "";
}

// One row per imported source: when it last succeeded and what the last run changed.
public class SyncState
{
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";        // "ok" | "failed"
    public DateTime? LastSyncedAt { get; set; }     // last successful run
    public DateTime? LastAttemptAt { get; set; }    // last run, successful or not
    public int Rows { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public string? Error { get; set; }
}
