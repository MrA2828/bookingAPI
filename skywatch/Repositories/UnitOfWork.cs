using SkyWatch.Data;

namespace SkyWatch.Repositories;

public class UnitOfWork(
    AppDbContext db,
    IUserRepository users,
    IAirportRepository airports,
    IAirlineRepository airlines,
    ICountryRepository countries,
    ISyncStateRepository syncStates) : IUnitOfWork
{
    public IUserRepository Users { get; } = users;
    public IAirportRepository Airports { get; } = airports;
    public IAirlineRepository Airlines { get; } = airlines;
    public ICountryRepository Countries { get; } = countries;
    public ISyncStateRepository SyncStates { get; } = syncStates;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public void DiscardChanges() => db.ChangeTracker.Clear();
}
