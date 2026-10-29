using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public interface ISyncStateRepository
{
    Task<SyncState?> FindAsync(string source, CancellationToken ct);
    void Add(SyncState state);
    Task<IReadOnlyList<SyncState>> ListAsync(CancellationToken ct);
}
