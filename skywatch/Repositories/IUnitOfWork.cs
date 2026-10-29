namespace SkyWatch.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IAirportRepository Airports { get; }
    IAirlineRepository Airlines { get; }
    ICountryRepository Countries { get; }
    ISyncStateRepository SyncStates { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Drops everything tracked but not yet saved (used to discard a half-applied sync after a failure).
    void DiscardChanges();
}
