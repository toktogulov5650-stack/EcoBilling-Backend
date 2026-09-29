using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class RefreshSessionRepository(EcoBillingDbContext dbContext)
    : IRefreshSessionRepository
{
    public async Task CreateAsync(
        RefreshSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        dbContext.RefreshSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshSessionRotationResult> RotateAsync(
        string currentTokenHash,
        RefreshSessionId replacementId,
        string replacementTokenHash,
        DateTimeOffset now,
        DateTimeOffset replacementExpiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTokenHash);
        ArgumentNullException.ThrowIfNull(replacementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementTokenHash);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        var current = await dbContext.RefreshSessions
            .FromSqlInterpolated(
                $"SELECT * FROM identity.refresh_sessions WHERE token_hash = {currentTokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null || current.RevokedAt is not null || current.IsExpired(now))
        {
            await transaction.CommitAsync(cancellationToken);
            return new RefreshSessionRotationResult(RefreshSessionRotationOutcome.Invalid);
        }

        if (current.ConsumedAt is not null)
        {
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RefreshSessionRotationResult(RefreshSessionRotationOutcome.Reused);
        }

        var replacement = RefreshSession.Create(
            replacementId,
            current.UserId,
            current.FamilyId,
            replacementTokenHash,
            now,
            replacementExpiresAt);
        if (replacement.IsFailure)
        {
            throw new ArgumentException(
                replacement.Error.Description,
                nameof(replacementTokenHash));
        }

        current.Consume(now, replacementId);
        dbContext.RefreshSessions.Add(replacement.Value);
        var user = await dbContext.UserAccounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == current.UserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshSessionRotationResult(
            RefreshSessionRotationOutcome.Rotated,
            user.Id,
            user.Role);
    }

    public async Task RevokeAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        var session = await dbContext.RefreshSessions
            .FromSqlInterpolated(
                $"SELECT * FROM identity.refresh_sessions WHERE token_hash = {tokenHash} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (session is not null)
        {
            await RevokeFamilyAsync(session.FamilyId, now, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<UserSessionRevocationResult> RevokeAllForUserAsync(
        UserId userId,
        string actorId,
        string correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var utcNow = now.ToUniversalTime();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var userExists = await dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(account => account.Id == userId, cancellationToken);
        if (!userExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new UserSessionRevocationResult(
                UserSessionRevocationOutcome.UserNotFound);
        }

        var revokedSessions = await dbContext.RefreshSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    utcNow),
                cancellationToken);

        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "identity.user.sessions_revoked",
                "UserAccount",
                userId.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        userId = userId.Value,
                        revokedSessions
                    }),
                correlationId,
                utcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UserSessionRevocationResult(
            UserSessionRevocationOutcome.Revoked,
            revokedSessions);
    }

    private Task<int> RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.RefreshSessions
            .Where(session => session.FamilyId == familyId && session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    now.ToUniversalTime()),
                cancellationToken);
}
