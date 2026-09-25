using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence;

public sealed class EcoBillingDbContext(DbContextOptions<EcoBillingDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<Resident> Residents => Set<Resident>();

    public DbSet<Controller> Controllers => Set<Controller>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<Meter> Meters => Set<Meter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoBillingDbContext).Assembly);
    }
}
