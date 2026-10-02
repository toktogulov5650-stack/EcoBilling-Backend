using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Domain;

public sealed class DirectorPasswordResetOperation
{
    public const int MaximumIdempotencyKeyLength = 200;
    public const int RequestFingerprintLength = 64;
    public const int MaximumNormalizedEmailLength = 320;

    private DirectorPasswordResetOperation()
    {
        Id = null!;
        IdempotencyKey = string.Empty;
        RequestFingerprint = string.Empty;
        NormalizedEmail = string.Empty;
    }

    private DirectorPasswordResetOperation(
        DirectorPasswordResetOperationId id,
        string idempotencyKey,
        string requestFingerprint,
        string normalizedEmail,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        NormalizedEmail = normalizedEmail;
        CreatedAt = createdAt;
    }

    public DirectorPasswordResetOperationId Id { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string RequestFingerprint { get; private set; }

    public string NormalizedEmail { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<DirectorPasswordResetOperation> Create(
        DirectorPasswordResetOperationId id,
        string? idempotencyKey,
        string? requestFingerprint,
        string? normalizedEmail,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            return Result<DirectorPasswordResetOperation>.Failure(
                DirectorProvisioningErrors.InvalidIdempotencyKey);
        }

        if (requestFingerprint is null ||
            requestFingerprint.Length != RequestFingerprintLength ||
            requestFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            return Result<DirectorPasswordResetOperation>.Failure(
                DirectorProvisioningErrors.InvalidRequestFingerprint);
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            normalizedEmail.Length > MaximumNormalizedEmailLength)
        {
            return Result<DirectorPasswordResetOperation>.Failure(
                IdentityErrors.InvalidLogin);
        }

        return Result<DirectorPasswordResetOperation>.Success(
            new DirectorPasswordResetOperation(
                id,
                idempotencyKey,
                requestFingerprint.ToUpperInvariant(),
                normalizedEmail,
                createdAt.ToUniversalTime()));
    }
}
