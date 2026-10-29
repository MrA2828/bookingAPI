namespace SkyWatch.Features.ReferenceData.Sources;

public class ReferenceDataOptions
{
    public const string Section = "ReferenceData";

    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);       // how often to refresh a source
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromHours(1);   // wait after a failed run

    // OurAirports is updated nightly. OpenFlights' airlines.dat has not changed since 2017,
    // so treat it as a baseline until a fresher source replaces it.
    public string AirportsUrl { get; set; } = "https://davidmegginson.github.io/ourairports-data/airports.csv";
    public string CountriesUrl { get; set; } = "https://davidmegginson.github.io/ourairports-data/countries.csv";
    public string AirlinesUrl { get; set; } = "https://raw.githubusercontent.com/jpatokal/openflights/master/data/airlines.dat";
}
