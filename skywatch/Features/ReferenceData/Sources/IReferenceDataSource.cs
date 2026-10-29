namespace SkyWatch.Features.ReferenceData.Sources;

// Downloads and parses the external datasets. Swap the implementation to change where data comes from.
public interface IReferenceDataSource
{
    Task<IReadOnlyList<AirportRecord>> FetchAirportsAsync(CancellationToken ct);
    Task<IReadOnlyList<AirlineRecord>> FetchAirlinesAsync(CancellationToken ct);
    Task<IReadOnlyList<CountryRecord>> FetchCountriesAsync(CancellationToken ct);
}
