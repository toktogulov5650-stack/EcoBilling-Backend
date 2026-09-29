using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.Modules.Payments.Features.Abstractions;

public sealed record PaymentRegistrationPersistenceResult(
    PaymentRegistrationPersistenceOutcome Outcome,
    Payment? Payment = null,
    decimal Overpayment = 0m,
    bool IsReplay = false);

public enum PaymentRegistrationPersistenceOutcome
{
    Registered = 0,
    Replayed = 1,
    AccountNotFound = 2,
    IdempotencyConflict = 3
}
