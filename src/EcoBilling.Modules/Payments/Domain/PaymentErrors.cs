using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Payments.Domain;

public static class PaymentErrors
{
    public static Error InvalidAmount { get; } = new(
        "payment.invalid_amount",
        "The payment amount is invalid.",
        ErrorType.Validation);

    public static Error InvalidIdempotencyKey { get; } = new(
        "payment.invalid_idempotency_key",
        "The payment idempotency key is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "payment.not_found",
        "The payment was not found.",
        ErrorType.NotFound);

    public static Error AccountNotFound { get; } = new(
        "payment.account_not_found",
        "The account was not found.",
        ErrorType.NotFound);

    public static Error IdempotencyConflict { get; } = new(
        "payment.idempotency_conflict",
        "The payment idempotency key was already used with different payment data.",
        ErrorType.Conflict);
}
