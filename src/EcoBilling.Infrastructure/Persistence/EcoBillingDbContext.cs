using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Tariffs.Domain;
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

    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    public DbSet<Tariff> Tariffs => Set<Tariff>();

    public DbSet<TariffVersion> TariffVersions => Set<TariffVersion>();

    public DbSet<Charge> Charges => Set<Charge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoBillingDbContext).Assembly);
    }
}
