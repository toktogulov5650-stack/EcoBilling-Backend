using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Billing.Domain;

public sealed class Charge
{
    private Charge()
    {
        Id = null!;
        AccountId = null!;
        TariffVersionId = null!;
    }

    private Charge(
        ChargeId id,
        AccountId accountId,
        TariffVersionId tariffVersionId,
        BillingPeriod period,
        decimal amount,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        TariffVersionId = tariffVersionId;
        PeriodStart = period.Start;
        PeriodEnd = period.End;
        Amount = amount;
        CreatedAt = createdAt;
    }

    public ChargeId Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public TariffVersionId TariffVersionId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public DateOnly PeriodEnd { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Charge> Create(
        ChargeId id,
        AccountId accountId,
        TariffVersionId tariffVersionId,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal amount,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(tariffVersionId);

        var period = BillingPeriod.Create(periodStart, periodEnd);
        if (period.IsFailure)
        {
            return Result<Charge>.Failure(period.Error);
        }

        return Result<Charge>.Success(
            new Charge(
                id,
                accountId,
                tariffVersionId,
                period.Value,
                amount,
                createdAt.ToUniversalTime()));
    }
}
