using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface ICountryRepository
{
    Task<Dictionary<string, Country>> GetAllByCodeAsync(CancellationToken ct);
    void Add(Country country);
    Task<IReadOnlyList<Country>> ListAsync(CancellationToken ct);
}
