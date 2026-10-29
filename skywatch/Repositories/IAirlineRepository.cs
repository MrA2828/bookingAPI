using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface IAirlineRepository
{
    Task<Dictionary<int, Airline>> GetAllByIdAsync(CancellationToken ct);
    void Add(Airline airline);
    Task<IReadOnlyList<Airline>> SearchAsync(string? q, int limit, int offset, CancellationToken ct);
}
