namespace EcoBilling.Modules.Billing.Contracts;

public sealed record ChargeDetails(
    Guid Id,
    Guid AccountId,
    Guid TariffVersionId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    DateTimeOffset CreatedAt);
