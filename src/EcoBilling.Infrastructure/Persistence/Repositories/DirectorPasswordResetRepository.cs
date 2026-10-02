using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class DirectorPasswordResetRepository(EcoBillingDbContext dbContext)
    : IDirectorPasswordResetRepository
{
    private const long PasswordResetLockId = 3_114_606_888_014_530_912;

    public async Task<DirectorPasswordResetPersistenceResult> ResetAsync(
        DirectorPasswordResetOperation operation,
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

        var existingOperation = await dbContext.DirectorPasswordResetOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.IdempotencyKey == operation.IdempotencyKey,
                cancellationToken);
        if (existingOperation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOperation.RequestFingerprint == operation.RequestFingerprint
                ? new DirectorPasswordResetPersistenceResult(
                    DirectorPasswordResetPersistenceOutcome.Replayed,
                    existingOperation.Id)
                : new DirectorPasswordResetPersistenceResult(
                    DirectorPasswordResetPersistenceOutcome.IdempotencyConflict);
        }

        var userAccount = await dbContext.UserAccounts.SingleOrDefaultAsync(
            account =>
                account.Role == UserRole.Director &&
                account.LoginIdentity.Type == LoginType.Email &&
                account.LoginIdentity.NormalizedValue == operation.NormalizedEmail,
            cancellationToken);
        if (userAccount is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new DirectorPasswordResetPersistenceResult(
                DirectorPasswordResetPersistenceOutcome.DirectorNotFound);
        }

        var director = await dbContext.Directors
            .AsNoTracking()
            .SingleAsync(
                profile => profile.UserId == userAccount.Id,
                cancellationToken);

        userAccount.ResetDirectorPassword(newPasswordHash);
        await dbContext.RefreshSessions
            .Where(session =>
                session.UserId == userAccount.Id &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    operation.CreatedAt),
                cancellationToken);

        dbContext.DirectorPasswordResetOperations.Add(operation);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "InternalService",
                actorId,
                "identity.director.password_reset",
                "Director",
                director.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        directorId = director.Id.Value,
                        userId = userAccount.Id.Value,
                        operationId = operation.Id.Value,
                        refreshSessionsRevoked = true
                    }),
                correlationId,
                operation.CreatedAt));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DirectorPasswordResetPersistenceResult(
            DirectorPasswordResetPersistenceOutcome.Reset,
            operation.Id);
    }
}
