using SkyWatch.Features.ReferenceData.Sources;

namespace SkyWatch.Entities;

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
