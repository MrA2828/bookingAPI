namespace BookingApi.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
