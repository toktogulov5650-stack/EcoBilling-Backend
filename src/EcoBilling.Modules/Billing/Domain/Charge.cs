using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Billing.Domain;

public sealed class Charge
{
    public const string V1CalculationVersion = "v1";
    public const string V1Currency = "KGS";

    private Charge()
    {
        Id = null!;
        AccountId = null!;
        TariffVersionId = null!;
        CalculationVersion = V1CalculationVersion;
        Currency = V1Currency;
    }

    private Charge(
        ChargeId id,
        AccountId accountId,
        TariffVersionId tariffVersionId,
        BillingPeriod period,
        decimal amount,
        DateTimeOffset createdAt,
        MeterReadingId? previousReadingId,
        MeterReadingId? currentReadingId,
        decimal consumption,
        string calculationVersion,
        string currency)
    {
        Id = id;
        AccountId = accountId;
        TariffVersionId = tariffVersionId;
        PeriodStart = period.Start;
        PeriodEnd = period.End;
        Amount = amount;
        CreatedAt = createdAt.ToUniversalTime();
        PreviousReadingId = previousReadingId;
        CurrentReadingId = currentReadingId;
        Consumption = consumption;
        CalculationVersion = calculationVersion;
        Currency = currency;
    }

    public ChargeId Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public TariffVersionId TariffVersionId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public DateOnly PeriodEnd { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public MeterReadingId? PreviousReadingId { get; private set; }

    public MeterReadingId? CurrentReadingId { get; private set; }

    public decimal Consumption { get; private set; }

    public string CalculationVersion { get; private set; }

    public string Currency { get; private set; }

    public static Result<Charge> Create(
        ChargeId id,
        AccountId accountId,
        TariffVersionId tariffVersionId,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal amount,
        DateTimeOffset createdAt) =>
        Create(
            id,
            accountId,
            tariffVersionId,
            periodStart,
            periodEnd,
            amount,
            createdAt,
            previousReadingId: null,
            currentReadingId: null,
            consumption: 0m,
            V1CalculationVersion,
            V1Currency);

    public static Result<Charge> Create(
        ChargeId id,
        AccountId accountId,
        TariffVersionId tariffVersionId,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal amount,
        DateTimeOffset createdAt,
        MeterReadingId? previousReadingId,
        MeterReadingId? currentReadingId,
        decimal consumption,
        string calculationVersion,
        string currency)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(tariffVersionId);

        var period = BillingPeriod.Create(periodStart, periodEnd);
        if (period.IsFailure)
        {
            return Result<Charge>.Failure(period.Error);
        }

        if (amount < 0 || consumption < 0)
        {
            return Result<Charge>.Failure(ChargeErrors.InvalidAmount);
        }

        if (string.IsNullOrWhiteSpace(calculationVersion))
        {
            return Result<Charge>.Failure(ChargeErrors.InvalidCalculationVersion);
        }

        if (!string.Equals(currency, V1Currency, StringComparison.Ordinal))
        {
            return Result<Charge>.Failure(ChargeErrors.InvalidCurrency);
        }

        return Result<Charge>.Success(
            new Charge(
                id,
                accountId,
                tariffVersionId,
                period.Value,
                amount,
                createdAt,
                previousReadingId,
                currentReadingId,
                consumption,
                calculationVersion,
                currency));
    }
}
