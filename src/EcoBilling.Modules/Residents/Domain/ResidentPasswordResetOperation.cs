using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Domain;

public sealed class ResidentPasswordResetOperation
{
    public const int MaximumIdempotencyKeyLength = 200;
    public const int RequestFingerprintLength = 64;

    private ResidentPasswordResetOperation()
    {
        Id = null!;
        IdempotencyKey = string.Empty;
        RequestFingerprint = string.Empty;
        ResidentId = null!;
    }

    private ResidentPasswordResetOperation(
        ResidentPasswordResetOperationId id,
        string idempotencyKey,
        string requestFingerprint,
        ResidentId residentId,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        ResidentId = residentId;
        CreatedAt = createdAt;
    }

    public ResidentPasswordResetOperationId Id { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string RequestFingerprint { get; private set; }

    public ResidentId ResidentId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<ResidentPasswordResetOperation> Create(
        ResidentPasswordResetOperationId id,
        string? idempotencyKey,
        string? requestFingerprint,
        ResidentId residentId,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(residentId);

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            return Result<ResidentPasswordResetOperation>.Failure(
                ResidentErrors.InvalidPasswordResetIdempotencyKey);
        }

        if (requestFingerprint is null ||
            requestFingerprint.Length != RequestFingerprintLength ||
            requestFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            return Result<ResidentPasswordResetOperation>.Failure(
                ResidentErrors.InvalidPasswordResetRequestFingerprint);
        }

        return Result<ResidentPasswordResetOperation>.Success(
            new ResidentPasswordResetOperation(
                id,
                idempotencyKey,
                requestFingerprint.ToUpperInvariant(),
                residentId,
                createdAt.ToUniversalTime()));
    }
}
