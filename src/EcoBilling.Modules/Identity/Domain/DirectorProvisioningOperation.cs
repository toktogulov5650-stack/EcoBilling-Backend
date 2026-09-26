using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Domain;

public sealed class DirectorProvisioningOperation
{
    public const int MaximumIdempotencyKeyLength = 200;
    public const int RequestFingerprintLength = 64;

    private DirectorProvisioningOperation()
    {
        Id = null!;
        IdempotencyKey = string.Empty;
        RequestFingerprint = string.Empty;
        DirectorId = null!;
    }

    private DirectorProvisioningOperation(
        DirectorProvisioningOperationId id,
        string idempotencyKey,
        string requestFingerprint,
        DirectorId directorId,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        DirectorId = directorId;
        CreatedAt = createdAt;
    }

    public DirectorProvisioningOperationId Id { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string RequestFingerprint { get; private set; }

    public DirectorId DirectorId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<DirectorProvisioningOperation> Create(
        DirectorProvisioningOperationId id,
        string? idempotencyKey,
        string? requestFingerprint,
        DirectorId directorId,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(directorId);

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            return Result<DirectorProvisioningOperation>.Failure(
                DirectorProvisioningErrors.InvalidIdempotencyKey);
        }

        if (requestFingerprint is null ||
            requestFingerprint.Length != RequestFingerprintLength ||
            requestFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            return Result<DirectorProvisioningOperation>.Failure(
                DirectorProvisioningErrors.InvalidRequestFingerprint);
        }

        return Result<DirectorProvisioningOperation>.Success(
            new DirectorProvisioningOperation(
                id,
                idempotencyKey,
                requestFingerprint.ToUpperInvariant(),
                directorId,
                createdAt.ToUniversalTime()));
    }
}
