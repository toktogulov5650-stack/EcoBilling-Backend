namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentChargeView(
    Guid ChargeId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Consumption,
    decimal Amount,
    string Currency,
    string CalculationVersion);
