using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;

namespace SkyWatch.Features.ReferenceData.Sources;

public class HttpReferenceDataSource(HttpClient http, IOptions<ReferenceDataOptions> options) : IReferenceDataSource
{
    private readonly ReferenceDataOptions _options = options.Value;

    public async Task<IReadOnlyList<AirportRecord>> FetchAirportsAsync(CancellationToken ct)
    {
        var rows = new List<AirportRecord>();
        await using var stream = await http.GetStreamAsync(_options.AirportsUrl, ct);
        using var csv = new CsvReader(new StreamReader(stream), Config(hasHeader: true));
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            // Columns are read by name so extra or reordered columns in the source do not break the import.
            if (!int.TryParse(csv.GetField("id"), out var id)
                || !TryDouble(csv.GetField("latitude_deg"), out var lat)
                || !TryDouble(csv.GetField("longitude_deg"), out var lon)
                || string.IsNullOrWhiteSpace(csv.GetField("name")))
                continue;

            rows.Add(new AirportRecord(
                id,
                csv.GetField("ident")?.Trim() ?? "",
                csv.GetField("type")?.Trim() ?? "",
                csv.GetField("name")!.Trim(),
                lat, lon,
                int.TryParse(csv.GetField("elevation_ft"), out var elevation) ? elevation : null,
                csv.GetField("continent")?.Trim() ?? "",
                csv.GetField("iso_country")?.Trim() ?? "",
                csv.GetField("iso_region")?.Trim() ?? "",
                Clean(csv.GetField("municipality")),
                csv.GetField("scheduled_service") == "yes",
                Code(csv.GetField("icao_code"), 4),
                Code(csv.GetField("iata_code"), 3)));
        }
        return rows;
    }

    public async Task<IReadOnlyList<CountryRecord>> FetchCountriesAsync(CancellationToken ct)
    {
        var rows = new List<CountryRecord>();
        await using var stream = await http.GetStreamAsync(_options.CountriesUrl, ct);
        using var csv = new CsvReader(new StreamReader(stream), Config(hasHeader: true));
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            var code = csv.GetField("code")?.Trim().ToUpperInvariant();
            var name = Clean(csv.GetField("name"));
            if (code is { Length: 2 } && name is not null)
                rows.Add(new CountryRecord(code, name, csv.GetField("continent")?.Trim() ?? ""));
        }
        return rows;
    }

    // airlines.dat has no header: id, name, alias, iata, icao, callsign, country, active. Nulls are \N.
    public async Task<IReadOnlyList<AirlineRecord>> FetchAirlinesAsync(CancellationToken ct)
    {
        var rows = new List<AirlineRecord>();
        await using var stream = await http.GetStreamAsync(_options.AirlinesUrl, ct);
        using var csv = new CsvReader(new StreamReader(stream), Config(hasHeader: false));

        while (await csv.ReadAsync())
        {
            var name = Clean(csv.GetField(1));
            if (!int.TryParse(csv.GetField(0), out var id) || id <= 0 || name is null
                || name is "Unknown" or "Private flight" || csv.GetField(7) != "Y")   // only airlines flagged active
                continue;

            rows.Add(new AirlineRecord(id, name, Clean(csv.GetField(2)), Code(csv.GetField(3), 2),
                Code(csv.GetField(4), 3), Clean(csv.GetField(5)), Clean(csv.GetField(6))));
        }
        return rows;
    }

    private static CsvConfiguration Config(bool hasHeader) => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = hasHeader,
        MissingFieldFound = null,
        BadDataFound = null,
    };

    private static bool TryDouble(string? s, out double value) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    // Empty and placeholder values (\N, N/A, -) become null.
    private static string? Clean(string? s)
    {
        s = s?.Trim();
        return string.IsNullOrEmpty(s) || s is "\\N" or "N/A" or "-" ? null : s;
    }

    // IATA/ICAO codes: upper-case, and only accepted when they have the expected length.
    private static string? Code(string? s, int length)
    {
        s = Clean(s)?.ToUpperInvariant();
        return s is not null && s.Length == length ? s : null;
    }
}
