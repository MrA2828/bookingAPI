using Microsoft.EntityFrameworkCore;
using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.Repositories;

public class SyncStateRepository(AppDbContext db) : ISyncStateRepository
{
    public Task<SyncState?> FindAsync(string source, CancellationToken ct) => db.SyncStates.FirstOrDefaultAsync(s => s.Source == source, ct);

    public void Add(SyncState state) => db.SyncStates.Add(state);

    public async Task<IReadOnlyList<SyncState>> ListAsync(CancellationToken ct) =>
        await db.SyncStates.AsNoTracking().OrderBy(s => s.Source).ToListAsync(ct);
}
