using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Meters.Domain;

public sealed class Meter
{
    private Meter()
    {
        Id = null!;
        AccountId = null!;
        SerialNumber = null!;
    }

    private Meter(
        MeterId id,
        AccountId accountId,
        MeterSerialNumber serialNumber,
        DateTimeOffset installedAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        SerialNumber = serialNumber;
        InstalledAt = installedAt;
        CreatedAt = createdAt;
    }

    public MeterId Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public MeterSerialNumber SerialNumber { get; private set; }

    public DateTimeOffset InstalledAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Meter> Create(
        MeterId id,
        AccountId accountId,
        string? serialNumber,
        DateTimeOffset installedAt,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(accountId);

        var normalizedSerialNumber = MeterSerialNumber.Create(serialNumber);
        if (normalizedSerialNumber.IsFailure)
        {
            return Result<Meter>.Failure(normalizedSerialNumber.Error);
        }

        return Result<Meter>.Success(
            new Meter(
                id,
                accountId,
                normalizedSerialNumber.Value,
                installedAt.ToUniversalTime(),
                createdAt.ToUniversalTime()));
    }
}
