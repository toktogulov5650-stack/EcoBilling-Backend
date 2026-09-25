namespace EcoBilling.Modules.Payments.Contracts;

public sealed record PaymentDetails(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey,
    DateTimeOffset PaidAt,
    DateTimeOffset CreatedAt);
