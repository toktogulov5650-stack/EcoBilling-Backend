using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class TariffVersionRepository(EcoBillingDbContext dbContext)
    : ITariffVersionRepository
{
    private const long TariffVersionMutationLockId = 5_681_421_804_332_574_199;

    public Task<TariffVersion?> GetByIdAsync(
        TariffVersionId tariffVersionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariffVersionId);

        return dbContext.TariffVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                version => version.Id == tariffVersionId,
                cancellationToken);
    }

    public Task<TariffVersion?> GetEffectiveAsync(
        TariffId tariffId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariffId);

        return dbContext.TariffVersions
            .AsNoTracking()
            .Where(
                version =>
                    version.TariffId == tariffId &&
                    version.EffectiveFrom <= date &&
                    (version.EffectiveTo == null || date < version.EffectiveTo))
            .OrderByDescending(version => version.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TariffVersion>> ListByTariffIdAsync(
        TariffId tariffId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariffId);

        return await dbContext.TariffVersions
            .AsNoTracking()
            .Where(version => version.TariffId == tariffId)
            .OrderBy(version => version.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<TariffVersionPersistenceResult> CreateAsync(
        TariffVersion version,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({TariffVersionMutationLockId})",
            cancellationToken);

        var tariffExists = await dbContext.Tariffs
            .AsNoTracking()
            .AnyAsync(tariff => tariff.Id == version.TariffId, cancellationToken);
        if (!tariffExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new TariffVersionPersistenceResult(
                TariffVersionPersistenceOutcome.TariffNotFound);
        }

        var overlaps = await dbContext.TariffVersions
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.TariffId == version.TariffId &&
                    existing.EffectiveFrom < (version.EffectiveTo ?? DateOnly.MaxValue) &&
                    version.EffectiveFrom < (existing.EffectiveTo ?? DateOnly.MaxValue),
                cancellationToken);
        if (overlaps)
        {
            await transaction.CommitAsync(cancellationToken);
            return new TariffVersionPersistenceResult(
                TariffVersionPersistenceOutcome.OverlappingPeriod);
        }

        dbContext.TariffVersions.Add(version);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "tariffs.version.created",
                "TariffVersion",
                version.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        tariffVersionId = version.Id.Value,
                        tariffId = version.TariffId.Value,
                        rate = version.Rate.Value,
                        effectiveFrom = version.EffectiveFrom,
                        effectiveTo = version.EffectiveTo
                    }),
                correlationId,
                version.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new TariffVersionPersistenceResult(
            TariffVersionPersistenceOutcome.Created,
            version.Id);
    }
}
