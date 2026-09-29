namespace EcoBilling.Modules.Payments.Features.RegisterManual;

public sealed record RegisterManualPaymentResult(
    Guid PaymentId,
    Guid AccountId,
    decimal Amount,
    DateTimeOffset PaidAt,
    decimal Overpayment,
    bool IsReplay);
