using Microsoft.EntityFrameworkCore;
using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public class CountryRepository(AppDbContext db) : ICountryRepository
{
    public Task<Dictionary<string, Country>> GetAllByCodeAsync(CancellationToken ct) => db.Countries.ToDictionaryAsync(c => c.Code, ct);

    public void Add(Country country) => db.Countries.Add(country);

    public async Task<IReadOnlyList<Country>> ListAsync(CancellationToken ct) =>
        await db.Countries.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
}
