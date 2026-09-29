using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class TariffRepository(EcoBillingDbContext dbContext)
    : ITariffRepository
{
    private const long TariffMutationLockId = 7_109_855_460_812_447_553;

    public Task<Tariff?> GetByIdAsync(
        TariffId tariffId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariffId);

        return dbContext.Tariffs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tariff => tariff.Id == tariffId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Tariff>> ListAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Tariffs
            .AsNoTracking()
            .OrderBy(tariff => tariff.Name)
            .ToListAsync(cancellationToken);

    public async Task<TariffPersistenceResult> CreateAsync(
        Tariff tariff,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariff);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({TariffMutationLockId})",
            cancellationToken);

        var exists = await dbContext.Tariffs
            .AsNoTracking()
            .AnyAsync(existing => existing.Name == tariff.Name, cancellationToken);
        if (exists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new TariffPersistenceResult(TariffPersistenceOutcome.NameAlreadyExists);
        }

        dbContext.Tariffs.Add(tariff);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "tariffs.tariff.created",
                "Tariff",
                tariff.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        tariffId = tariff.Id.Value,
                        name = tariff.Name.Value
                    }),
                correlationId,
                tariff.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new TariffPersistenceResult(
            TariffPersistenceOutcome.Created,
            tariff.Id);
    }
}
