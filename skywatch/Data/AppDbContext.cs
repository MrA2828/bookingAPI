using SkyWatch.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SkyWatch.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<User>().ToTable("users");

        builder.Entity<Airport>(e =>
        {
            e.ToTable("airports");
            e.Property(a => a.Id).ValueGeneratedNever();   // ids come from the source dataset
            e.HasIndex(a => a.IataCode);
            e.HasIndex(a => a.IcaoCode);
            e.HasIndex(a => a.Ident);
            e.HasIndex(a => a.CountryCode);
            e.HasIndex(a => new { a.Latitude, a.Longitude });
        });

        builder.Entity<Airline>(e =>
        {
            e.ToTable("airlines");
            e.Property(a => a.Id).ValueGeneratedNever();
            e.HasIndex(a => a.IataCode);
            e.HasIndex(a => a.IcaoCode);
        });

        builder.Entity<Country>(e =>
        {
            e.ToTable("countries");
            e.HasKey(c => c.Code);
        });

        builder.Entity<SyncState>(e =>
        {
            e.ToTable("sync_states");
            e.HasKey(s => s.Source);
        });
    }
}
