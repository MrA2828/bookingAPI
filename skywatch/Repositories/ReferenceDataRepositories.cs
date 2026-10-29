using Microsoft.EntityFrameworkCore;
using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface IAirportRepository
{
    Task<Dictionary<int, Airport>> GetAllByIdAsync(CancellationToken ct);      // tracked, used by the sync
    void Add(Airport airport);
    Task<IReadOnlyList<Airport>> SearchAsync(string? q, string? country, string? type, int limit, int offset, CancellationToken ct);
    Task<Airport?> FindByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Airport>> InBoundingBoxAsync(double minLat, double maxLat, double minLon, double maxLon, bool scheduledOnly, CancellationToken ct);
}

public interface IAirlineRepository
{
    Task<Dictionary<int, Airline>> GetAllByIdAsync(CancellationToken ct);
    void Add(Airline airline);
    Task<IReadOnlyList<Airline>> SearchAsync(string? q, int limit, int offset, CancellationToken ct);
}

public interface ICountryRepository
{
    Task<Dictionary<string, Country>> GetAllByCodeAsync(CancellationToken ct);
    void Add(Country country);
    Task<IReadOnlyList<Country>> ListAsync(CancellationToken ct);
}

public interface ISyncStateRepository
{
    Task<SyncState?> FindAsync(string source, CancellationToken ct);
    void Add(SyncState state);
    Task<IReadOnlyList<SyncState>> ListAsync(CancellationToken ct);
}

public class AirportRepository(AppDbContext db) : IAirportRepository
{
    public Task<Dictionary<int, Airport>> GetAllByIdAsync(CancellationToken ct) => db.Airports.ToDictionaryAsync(a => a.Id, ct);

    public void Add(Airport airport) => db.Airports.Add(airport);

    public async Task<IReadOnlyList<Airport>> SearchAsync(string? q, string? country, string? type, int limit, int offset, CancellationToken ct)
    {
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

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            var code = q.ToUpperInvariant();
            var pattern = $"%{q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(a => a.IataCode == code || a.IcaoCode == code || a.Ident == code
                || EF.Functions.ILike(a.Name, pattern, "\\") || (a.Municipality != null && EF.Functions.ILike(a.Municipality, pattern, "\\")));

            // Exact code matches first, then bigger airports.
            return await query
                .OrderBy(a => a.IataCode == code || a.IcaoCode == code || a.Ident == code ? 0 : 1)
                .ThenBy(a => a.Type == "large_airport" ? 0 : a.Type == "medium_airport" ? 1 : 2)
                .ThenBy(a => a.Name)
                .Skip(offset).Take(limit).ToListAsync(ct);
        }

        return await query
            .OrderBy(a => a.Type == "large_airport" ? 0 : a.Type == "medium_airport" ? 1 : 2)
            .ThenBy(a => a.Name)
            .Skip(offset).Take(limit).ToListAsync(ct);
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

public class AirlineRepository(AppDbContext db) : IAirlineRepository
{
    public Task<Dictionary<int, Airline>> GetAllByIdAsync(CancellationToken ct) => db.Airlines.ToDictionaryAsync(a => a.Id, ct);

    public void Add(Airline airline) => db.Airlines.Add(airline);

    public async Task<IReadOnlyList<Airline>> SearchAsync(string? q, int limit, int offset, CancellationToken ct)
    {
        var query = db.Airlines.AsNoTracking().Where(a => a.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            var code = q.ToUpperInvariant();
            var pattern = $"%{q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(a => a.IataCode == code || a.IcaoCode == code || EF.Functions.ILike(a.Name, pattern, "\\"));

            // Exact code matches first.
            return await query.OrderBy(a => a.IataCode == code || a.IcaoCode == code ? 0 : 1).ThenBy(a => a.Name)
                .Skip(offset).Take(limit).ToListAsync(ct);
        }
        return await query.OrderBy(a => a.Name).Skip(offset).Take(limit).ToListAsync(ct);
    }
}

public class CountryRepository(AppDbContext db) : ICountryRepository
{
    public Task<Dictionary<string, Country>> GetAllByCodeAsync(CancellationToken ct) => db.Countries.ToDictionaryAsync(c => c.Code, ct);

    public void Add(Country country) => db.Countries.Add(country);

    public async Task<IReadOnlyList<Country>> ListAsync(CancellationToken ct) =>
        await db.Countries.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
}

public class SyncStateRepository(AppDbContext db) : ISyncStateRepository
{
    public Task<SyncState?> FindAsync(string source, CancellationToken ct) => db.SyncStates.FirstOrDefaultAsync(s => s.Source == source, ct);

    public void Add(SyncState state) => db.SyncStates.Add(state);

    public async Task<IReadOnlyList<SyncState>> ListAsync(CancellationToken ct) =>
        await db.SyncStates.AsNoTracking().OrderBy(s => s.Source).ToListAsync(ct);
}
