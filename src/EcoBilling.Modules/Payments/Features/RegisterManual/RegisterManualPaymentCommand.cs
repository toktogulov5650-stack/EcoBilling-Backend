using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Payments.Features.RegisterManual;

public sealed record RegisterManualPaymentCommand(
    AccountId AccountId,
    decimal Amount,
    string? IdempotencyKey,
    DateTimeOffset PaidAt,
    string ActorId,
    string CorrelationId);
