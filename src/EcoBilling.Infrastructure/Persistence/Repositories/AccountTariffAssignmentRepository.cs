using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class AccountTariffAssignmentRepository(EcoBillingDbContext dbContext)
    : IAccountTariffAssignmentRepository
{
    private const long AssignmentLockId = 3_909_501_239_888_220_175;

    public async Task<AccountTariffAssignmentPersistenceOutcome> AssignAsync(
        AccountTariffAssignment assignment,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({AssignmentLockId})",
            cancellationToken);

        if (!await dbContext.Accounts.AsNoTracking().AnyAsync(
                account => account.Id == assignment.AccountId,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return AccountTariffAssignmentPersistenceOutcome.AccountNotFound;
        }

        if (!await dbContext.Tariffs.AsNoTracking().AnyAsync(
                tariff => tariff.Id == assignment.TariffId,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return AccountTariffAssignmentPersistenceOutcome.TariffNotFound;
        }

        var overlaps = await dbContext.AccountTariffAssignments
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.AccountId == assignment.AccountId &&
                    existing.EffectiveFrom < (assignment.EffectiveTo ?? DateOnly.MaxValue) &&
                    assignment.EffectiveFrom < (existing.EffectiveTo ?? DateOnly.MaxValue),
                cancellationToken);
        if (overlaps)
        {
            await transaction.CommitAsync(cancellationToken);
            return AccountTariffAssignmentPersistenceOutcome.Overlap;
        }

        dbContext.AccountTariffAssignments.Add(assignment);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "tariffs.account_assignment.created",
                "AccountTariffAssignment",
                assignment.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        assignmentId = assignment.Id.Value,
                        accountId = assignment.AccountId.Value,
                        tariffId = assignment.TariffId.Value,
                        effectiveFrom = assignment.EffectiveFrom,
                        effectiveTo = assignment.EffectiveTo
                    }),
                correlationId,
                assignment.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccountTariffAssignmentPersistenceOutcome.Assigned;
    }

    public async Task<AccountTariffAssignmentCloseOutcome> CloseAsync(
        AccountId accountId,
        AccountTariffAssignmentId assignmentId,
        DateOnly effectiveTo,
        string actorId,
        string correlationId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(assignmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({AssignmentLockId})",
            cancellationToken);

        var assignment = await dbContext.AccountTariffAssignments
            .SingleOrDefaultAsync(
                existing =>
                    existing.Id == assignmentId &&
                    existing.AccountId == accountId,
                cancellationToken);
        if (assignment is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return AccountTariffAssignmentCloseOutcome.NotFound;
        }

        if (assignment.EffectiveTo is not null &&
            assignment.EffectiveTo.Value < effectiveTo)
        {
            await transaction.CommitAsync(cancellationToken);
            return AccountTariffAssignmentCloseOutcome.AlreadyClosed;
        }

        var before = new
        {
            assignmentId = assignment.Id.Value,
            accountId = assignment.AccountId.Value,
            tariffId = assignment.TariffId.Value,
            effectiveFrom = assignment.EffectiveFrom,
            effectiveTo = assignment.EffectiveTo
        };

        var closeResult = assignment.Close(effectiveTo);
        if (closeResult.IsFailure)
        {
            await transaction.CommitAsync(cancellationToken);
            return AccountTariffAssignmentCloseOutcome.InvalidEffectivePeriod;
        }

        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "tariffs.account_assignment.closed",
                "AccountTariffAssignment",
                assignment.Id.Value.ToString("D"),
                JsonSerializer.Serialize(before),
                JsonSerializer.Serialize(
                    new
                    {
                        assignmentId = assignment.Id.Value,
                        accountId = assignment.AccountId.Value,
                        tariffId = assignment.TariffId.Value,
                        effectiveFrom = assignment.EffectiveFrom,
                        effectiveTo = assignment.EffectiveTo
                    }),
                correlationId,
                changedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AccountTariffAssignmentCloseOutcome.Closed;
    }

    public Task<AccountTariffAssignment?> GetEffectiveAsync(
        AccountId accountId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return dbContext.AccountTariffAssignments
            .AsNoTracking()
            .Where(
                assignment =>
                    assignment.AccountId == accountId &&
                    assignment.EffectiveFrom <= date &&
                    (assignment.EffectiveTo == null || date < assignment.EffectiveTo))
            .OrderByDescending(assignment => assignment.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountTariffAssignment>> ListByAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await dbContext.AccountTariffAssignments
            .AsNoTracking()
            .Where(assignment => assignment.AccountId == accountId)
            .OrderBy(assignment => assignment.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }
}
