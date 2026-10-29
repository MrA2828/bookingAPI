using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface IAirlineRepository
{
    Task<Dictionary<int, Airline>> GetAllByIdAsync(CancellationToken ct);
    void Add(Airline airline);
    Task<IReadOnlyList<Airline>> SearchAsync(string? search, int pageNumber, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<string>> GetActiveIcaoCodesAsync(CancellationToken ct);
    Task<Airline?> FindByIcaoCodeAsync(string icaoCode, CancellationToken ct);
}
