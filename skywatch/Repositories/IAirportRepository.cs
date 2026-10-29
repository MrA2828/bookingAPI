using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface IAirportRepository
{
    Task<Dictionary<int, Airport>> GetAllByIdAsync(CancellationToken ct);      // tracked, used by the sync
    void Add(Airport airport);
    Task<IReadOnlyList<Airport>> SearchAsync(string? search, string? country, string? type, int pageNumber, int pageSize, CancellationToken ct);
    Task<Airport?> FindByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Airport>> InBoundingBoxAsync(double minLat, double maxLat, double minLon, double maxLon, bool scheduledOnly, CancellationToken ct);
}
