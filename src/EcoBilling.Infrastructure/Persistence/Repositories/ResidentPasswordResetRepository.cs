using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ResidentPasswordResetRepository(EcoBillingDbContext dbContext)
    : IResidentPasswordResetRepository
{
    private const long PasswordResetLockId = 5_091_142_804_113_761_010;

    public async Task<ResidentPasswordResetPersistenceResult> ResetAsync(
        ResidentPasswordResetOperation operation,
        string newPasswordHash,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({PasswordResetLockId})",
            cancellationToken);

        var existingOperation = await dbContext.ResidentPasswordResetOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.IdempotencyKey == operation.IdempotencyKey,
                cancellationToken);
        if (existingOperation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOperation.RequestFingerprint == operation.RequestFingerprint
                ? new ResidentPasswordResetPersistenceResult(
                    ResidentPasswordResetPersistenceOutcome.Replayed,
                    existingOperation.Id)
                : new ResidentPasswordResetPersistenceResult(
                    ResidentPasswordResetPersistenceOutcome.IdempotencyConflict);
        }

        var resident = await dbContext.Residents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.Id == operation.ResidentId,
                cancellationToken);
        if (resident is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ResidentPasswordResetPersistenceResult(
                ResidentPasswordResetPersistenceOutcome.ResidentNotFound);
        }

        var userAccount = await dbContext.UserAccounts.SingleAsync(
            account => account.Id == resident.UserId,
            cancellationToken);
        if (userAccount.Role is not UserRole.Resident)
        {
            throw new InvalidOperationException(
                "A Resident profile must reference an identity with the Resident role.");
        }

        userAccount.ResetResidentPassword(newPasswordHash);
        await dbContext.RefreshSessions
            .Where(session =>
                session.UserId == userAccount.Id &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    operation.CreatedAt),
                cancellationToken);

        dbContext.ResidentPasswordResetOperations.Add(operation);
        dbContext.AuditLogs.Add(
            CreateAudit(
                userAccount,
                operation,
                actorId,
                correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ResidentPasswordResetPersistenceResult(
            ResidentPasswordResetPersistenceOutcome.Reset,
            operation.Id);
    }

    private static AuditLog CreateAudit(
        UserAccount userAccount,
        ResidentPasswordResetOperation operation,
        string actorId,
        string correlationId) =>
        new(
            Guid.NewGuid(),
            "User",
            actorId,
            "residents.resident.password_reset",
            "Resident",
            operation.ResidentId.Value.ToString("D"),
            beforeData: null,
            JsonSerializer.Serialize(
                new
                {
                    residentId = operation.ResidentId.Value,
                    userId = userAccount.Id.Value,
                    operationId = operation.Id.Value,
                    refreshSessionsRevoked = true
                }),
            correlationId,
            operation.CreatedAt);
}
