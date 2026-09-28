using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Domain;

public sealed class RefreshSession
{
    public const int TokenHashLength = 64;

    private RefreshSession()
    {
        Id = null!;
        UserId = null!;
        TokenHash = string.Empty;
    }

    private RefreshSession(
        RefreshSessionId id,
        UserId userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public RefreshSessionId Id { get; private set; }

    public UserId UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshSessionId? ReplacedBySessionId { get; private set; }

    public static Result<RefreshSession> Create(
        RefreshSessionId id,
        UserId userId,
        Guid familyId,
        string? tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userId);

        if (familyId == Guid.Empty ||
            tokenHash is null ||
            tokenHash.Length != TokenHashLength ||
            tokenHash.Any(character => !Uri.IsHexDigit(character)))
        {
            return Result<RefreshSession>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var utcCreatedAt = createdAt.ToUniversalTime();
        var utcExpiresAt = expiresAt.ToUniversalTime();
        if (utcExpiresAt <= utcCreatedAt)
        {
            return Result<RefreshSession>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        return Result<RefreshSession>.Success(
            new RefreshSession(
                id,
                userId,
                familyId,
                tokenHash.ToUpperInvariant(),
                utcCreatedAt,
                utcExpiresAt));
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now.ToUniversalTime();

    public bool IsActive(DateTimeOffset now) =>
        !IsExpired(now) && ConsumedAt is null && RevokedAt is null;

    public void Consume(DateTimeOffset now, RefreshSessionId replacementId)
    {
        ArgumentNullException.ThrowIfNull(replacementId);
        if (!IsActive(now))
        {
            throw new InvalidOperationException("Only an active refresh session can be consumed.");
        }

        ConsumedAt = now.ToUniversalTime();
        ReplacedBySessionId = replacementId;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is null)
        {
            RevokedAt = now.ToUniversalTime();
        }
    }
}
