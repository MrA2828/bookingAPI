using Microsoft.EntityFrameworkCore;
using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public class AirlineRepository(AppDbContext db) : IAirlineRepository
{
    public Task<Dictionary<int, Airline>> GetAllByIdAsync(CancellationToken ct) => db.Airlines.ToDictionaryAsync(a => a.Id, ct);

    public void Add(Airline airline) => db.Airlines.Add(airline);

    public async Task<IReadOnlyList<Airline>> SearchAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = db.Airlines.AsNoTracking().Where(a => a.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            var code = search.ToUpperInvariant();
            var pattern = $"%{search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(a => a.IataCode == code || a.IcaoCode == code || EF.Functions.ILike(a.Name, pattern, "\\"));

            // Exact code matches first.
            return await query.OrderBy(a => a.IataCode == code || a.IcaoCode == code ? 0 : 1).ThenBy(a => a.Name)
                .Skip(offset).Take(limit).ToListAsync(ct);
        }
        return await query.OrderBy(a => a.Name).Skip(offset).Take(limit).ToListAsync(ct);
    }
}
