using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class MeterRepository(EcoBillingDbContext dbContext)
    : IMeterRepository
{
    private const long MeterMutationLockId = 7_381_470_608_212_774_521;

    public Task<Meter?> GetByIdAsync(
        MeterId meterId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meterId);

        return dbContext.Meters
            .AsNoTracking()
            .SingleOrDefaultAsync(
                meter => meter.Id == meterId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Meter>> ListByAccountIdAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await dbContext.Meters
            .AsNoTracking()
            .Where(meter => meter.AccountId == accountId)
            .OrderByDescending(meter => meter.IsActive)
            .ThenByDescending(meter => meter.InstalledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MeterPersistenceResult> CreateAsync(
        Meter meter,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meter);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({MeterMutationLockId})",
            cancellationToken);

        var accountExists = await dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.Id == meter.AccountId, cancellationToken);
        if (!accountExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new MeterPersistenceResult(MeterPersistenceOutcome.AccountNotFound);
        }

        dbContext.Meters.Add(meter);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "meters.meter.created",
                "Meter",
                meter.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        meterId = meter.Id.Value,
                        accountId = meter.AccountId.Value,
                        serialNumber = meter.SerialNumber.Value,
                        installedAt = meter.InstalledAt
                    }),
                correlationId,
                meter.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new MeterPersistenceResult(MeterPersistenceOutcome.Created, meter.Id);
    }

    public async Task<MeterPersistenceResult> ReplaceAsync(
        MeterId currentMeterId,
        Meter replacementMeter,
        string actorId,
        string correlationId,
        DateTimeOffset retiredAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentMeterId);
        ArgumentNullException.ThrowIfNull(replacementMeter);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({MeterMutationLockId})",
            cancellationToken);

        var current = await dbContext.Meters.SingleOrDefaultAsync(
            meter => meter.Id == currentMeterId,
            cancellationToken);
        if (current is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new MeterPersistenceResult(MeterPersistenceOutcome.MeterNotFound);
        }

        if (!current.IsActive)
        {
            await transaction.CommitAsync(cancellationToken);
            return new MeterPersistenceResult(MeterPersistenceOutcome.AlreadyRetired);
        }

        if (replacementMeter.AccountId != current.AccountId)
        {
            await transaction.CommitAsync(cancellationToken);
            return new MeterPersistenceResult(
                MeterPersistenceOutcome.ReplacementAccountMismatch);
        }

        var retireResult = current.Retire(retiredAt);
        if (retireResult.IsFailure)
        {
            await transaction.CommitAsync(cancellationToken);
            return new MeterPersistenceResult(MeterPersistenceOutcome.AlreadyRetired);
        }

        dbContext.Meters.Add(replacementMeter);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "meters.meter.replaced",
                "Meter",
                current.Id.Value.ToString("D"),
                JsonSerializer.Serialize(
                    new
                    {
                        meterId = current.Id.Value,
                        isActive = true
                    }),
                JsonSerializer.Serialize(
                    new
                    {
                        retiredMeterId = current.Id.Value,
                        replacementMeterId = replacementMeter.Id.Value,
                        retiredAt = current.RetiredAt
                    }),
                correlationId,
                retiredAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new MeterPersistenceResult(
            MeterPersistenceOutcome.Replaced,
            replacementMeter.Id);
    }
}
