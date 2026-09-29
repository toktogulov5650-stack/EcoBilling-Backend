namespace EcoBilling.Modules.Payments.Contracts;

public sealed record PaymentAllocationDetails(
    Guid ChargeId,
    decimal Amount);
