using Microsoft.EntityFrameworkCore;
using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public class AirportRepository(AppDbContext db) : IAirportRepository
{
    public Task<Dictionary<int, Airport>> GetAllByIdAsync(CancellationToken ct) => db.Airports.ToDictionaryAsync(a => a.Id, ct);

    public void Add(Airport airport) => db.Airports.Add(airport);

    public async Task<IReadOnlyList<Airport>> SearchAsync(string? search, string? country, string? type, int pageNumber, int pageSize, CancellationToken ct)
    {
        var skip = (pageNumber - 1) * pageSize;
        var query = db.Airports.AsNoTracking().Where(a => a.IsActive);

        if (!string.IsNullOrWhiteSpace(country))
        {
            var cc = country.Trim().ToUpperInvariant();
            query = query.Where(a => a.CountryCode == cc);
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.Trim();
            query = query.Where(a => a.Type == t);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            var code = search.ToUpperInvariant();
            var pattern = $"%{search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(a => a.IataCode == code || a.IcaoCode == code || a.Ident == code
                || EF.Functions.ILike(a.Name, pattern, "\\") || (a.Municipality != null && EF.Functions.ILike(a.Municipality, pattern, "\\")));

            // Exact code matches first, then bigger airports.
            return await query
                .OrderBy(a => a.IataCode == code || a.IcaoCode == code || a.Ident == code ? 0 : 1)
                .ThenBy(a => a.Type == "large_airport" ? 0 : a.Type == "medium_airport" ? 1 : 2)
                .ThenBy(a => a.Name)
                .Skip(skip).Take(pageSize).ToListAsync(ct);
        }

        return await query
            .OrderBy(a => a.Type == "large_airport" ? 0 : a.Type == "medium_airport" ? 1 : 2)
            .ThenBy(a => a.Name)
            .Skip(skip).Take(pageSize).ToListAsync(ct);
    }

    public Task<Airport?> FindByCodeAsync(string code, CancellationToken ct)
    {
        code = code.Trim().ToUpperInvariant();
        return db.Airports.AsNoTracking()
            .Where(a => a.IsActive && (a.IataCode == code || a.IcaoCode == code || a.Ident == code))
            .OrderBy(a => a.Type == "large_airport" ? 0 : a.Type == "medium_airport" ? 1 : 2)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Airport>> InBoundingBoxAsync(double minLat, double maxLat, double minLon, double maxLon, bool scheduledOnly, CancellationToken ct) =>
        await db.Airports.AsNoTracking()
            .Where(a => a.IsActive && a.Latitude >= minLat && a.Latitude <= maxLat && a.Longitude >= minLon && a.Longitude <= maxLon
                && (!scheduledOnly || a.ScheduledService))
            .ToListAsync(ct);
}
