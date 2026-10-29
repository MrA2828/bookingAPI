using BookingApi.Data;

namespace BookingApi.Repositories;

public class UnitOfWork(AppDbContext db, IUserRepository users) : IUnitOfWork
{
    public IUserRepository Users { get; } = users;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
