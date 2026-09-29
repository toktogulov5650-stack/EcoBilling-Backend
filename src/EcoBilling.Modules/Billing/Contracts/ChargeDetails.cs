namespace EcoBilling.Modules.Billing.Contracts;

public sealed record ChargeDetails(
    Guid Id,
    Guid AccountId,
    Guid TariffVersionId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    DateTimeOffset CreatedAt,
    Guid? PreviousReadingId = null,
    Guid? CurrentReadingId = null,
    decimal Consumption = 0m,
    string CalculationVersion = "v1",
    string Currency = "KGS");
