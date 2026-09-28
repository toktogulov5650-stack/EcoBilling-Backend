using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Outbox;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence;

public sealed class EcoBillingDbContext(DbContextOptions<EcoBillingDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<DirectorProfile> Directors => Set<DirectorProfile>();

    public DbSet<DirectorProvisioningOperation> DirectorProvisioningOperations =>
        Set<DirectorProvisioningOperation>();

    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    public DbSet<Resident> Residents => Set<Resident>();

    public DbSet<Controller> Controllers => Set<Controller>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<Meter> Meters => Set<Meter>();

    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    public DbSet<Tariff> Tariffs => Set<Tariff>();

    public DbSet<TariffVersion> TariffVersions => Set<TariffVersion>();

    public DbSet<Charge> Charges => Set<Charge>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    internal DbSet<InternalServiceTokenReplay> InternalServiceTokenReplays =>
        Set<InternalServiceTokenReplay>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditLogsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAuditLogsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoBillingDbContext).Assembly);
    }

    private void EnsureAuditLogsAreAppendOnly()
    {
        if (ChangeTracker.Entries<AuditLog>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Audit log entries are append-only and cannot be modified or deleted.");
        }
    }
}
