using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IRefreshSessionRepository
{
    Task CreateAsync(RefreshSession session, CancellationToken cancellationToken);

    Task<RefreshSessionRotationResult> RotateAsync(
        string currentTokenHash,
        RefreshSessionId replacementId,
        string replacementTokenHash,
        DateTimeOffset now,
        DateTimeOffset replacementExpiresAt,
        CancellationToken cancellationToken);

    Task RevokeAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public enum RefreshSessionRotationOutcome
{
    Invalid = 0,
    Rotated = 1,
    Reused = 2
}

public sealed record RefreshSessionRotationResult(
    RefreshSessionRotationOutcome Outcome,
    UserId? UserId = null,
    UserRole? Role = null);
