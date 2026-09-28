using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Domain;

public sealed class ControllerCreationOperation
{
    public const int MaximumIdempotencyKeyLength = 200;
    public const int RequestFingerprintLength = 64;

    private ControllerCreationOperation()
    {
        Id = null!;
        IdempotencyKey = string.Empty;
        RequestFingerprint = string.Empty;
        ControllerId = null!;
    }

    private ControllerCreationOperation(
        ControllerCreationOperationId id,
        string idempotencyKey,
        string requestFingerprint,
        ControllerId controllerId,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        ControllerId = controllerId;
        CreatedAt = createdAt;
    }

    public ControllerCreationOperationId Id { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string RequestFingerprint { get; private set; }

    public ControllerId ControllerId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<ControllerCreationOperation> Create(
        ControllerCreationOperationId id,
        string? idempotencyKey,
        string? requestFingerprint,
        ControllerId controllerId,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(controllerId);

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            return Result<ControllerCreationOperation>.Failure(
                ControllerErrors.InvalidCreationIdempotencyKey);
        }

        if (requestFingerprint is null ||
            requestFingerprint.Length != RequestFingerprintLength ||
            requestFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            return Result<ControllerCreationOperation>.Failure(
                ControllerErrors.InvalidCreationRequestFingerprint);
        }

        return Result<ControllerCreationOperation>.Success(
            new ControllerCreationOperation(
                id,
                idempotencyKey,
                requestFingerprint.ToUpperInvariant(),
                controllerId,
                createdAt.ToUniversalTime()));
    }
}
