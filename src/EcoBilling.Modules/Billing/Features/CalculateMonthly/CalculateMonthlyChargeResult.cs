namespace EcoBilling.Modules.Billing.Features.CalculateMonthly;

public sealed record CalculateMonthlyChargeResult(
    Guid ChargeId,
    Guid AccountId,
    Guid TariffVersionId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Consumption,
    decimal Amount,
    string Currency,
    string CalculationVersion,
    bool IsReplay);
