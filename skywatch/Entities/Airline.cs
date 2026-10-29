using SkyWatch.Features.ReferenceData.Sources;

namespace SkyWatch.Entities;

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
