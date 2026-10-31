using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface IAirportRepository
{
    Task<Dictionary<int, Airport>> GetAllByIdAsync(CancellationToken ct);      // tracked, used by the sync
    void Add(Airport airport);
    Task<IReadOnlyList<Airport>> SearchAsync(string? search, string? country, string? type, int pageNumber, int pageSize, CancellationToken ct);
    Task<Airport?> FindByCodeAsync(string code, CancellationToken ct);
    // Airports inside a rectangle, biggest first (large, medium, small), at most `limit`. `types` null or empty means all.
    Task<IReadOnlyList<Airport>> InViewAsync(double south, double north, double west, double east, bool scheduledOnly, string[]? types, int limit, CancellationToken ct);
    Task<IReadOnlyList<Airport>> InBoundingBoxAsync(double minLat, double maxLat, double minLon, double maxLon, bool scheduledOnly, CancellationToken ct);
}
