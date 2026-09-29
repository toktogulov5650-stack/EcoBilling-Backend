namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentPaymentView(
    Guid PaymentId,
    decimal Amount,
    DateTimeOffset PaidAt);
