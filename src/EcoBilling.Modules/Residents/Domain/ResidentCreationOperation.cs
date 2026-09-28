using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Domain;

public sealed class ResidentCreationOperation
{
    public const int MaximumIdempotencyKeyLength = 200;
    public const int RequestFingerprintLength = 64;

    private ResidentCreationOperation()
    {
        Id = null!;
        IdempotencyKey = string.Empty;
        RequestFingerprint = string.Empty;
        ResidentId = null!;
        AccountId = null!;
        AddressId = null!;
    }

    private ResidentCreationOperation(
        ResidentCreationOperationId id,
        string idempotencyKey,
        string requestFingerprint,
        ResidentId residentId,
        AccountId accountId,
        AddressId addressId,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        ResidentId = residentId;
        AccountId = accountId;
        AddressId = addressId;
        CreatedAt = createdAt;
    }

    public ResidentCreationOperationId Id { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string RequestFingerprint { get; private set; }

    public ResidentId ResidentId { get; private set; }

    public AccountId AccountId { get; private set; }

    public AddressId AddressId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<ResidentCreationOperation> Create(
        ResidentCreationOperationId id,
        string? idempotencyKey,
        string? requestFingerprint,
        ResidentId residentId,
        AccountId accountId,
        AddressId addressId,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(residentId);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(addressId);

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            return Result<ResidentCreationOperation>.Failure(
                ResidentErrors.InvalidCreationIdempotencyKey);
        }

        if (requestFingerprint is null ||
            requestFingerprint.Length != RequestFingerprintLength ||
            requestFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            return Result<ResidentCreationOperation>.Failure(
                ResidentErrors.InvalidCreationRequestFingerprint);
        }

        return Result<ResidentCreationOperation>.Success(
            new ResidentCreationOperation(
                id,
                idempotencyKey,
                requestFingerprint.ToUpperInvariant(),
                residentId,
                accountId,
                addressId,
                createdAt.ToUniversalTime()));
    }
}
