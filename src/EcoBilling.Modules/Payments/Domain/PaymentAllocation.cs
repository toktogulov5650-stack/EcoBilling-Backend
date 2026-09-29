using EcoBilling.Modules.Billing.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Domain;

public sealed class PaymentAllocation
{
    private PaymentAllocation()
    {
        Id = null!;
        PaymentId = null!;
        ChargeId = null!;
    }

    private PaymentAllocation(
        PaymentAllocationId id,
        PaymentId paymentId,
        ChargeId chargeId,
        decimal amount,
        DateTimeOffset createdAt)
    {
        Id = id;
        PaymentId = paymentId;
        ChargeId = chargeId;
        Amount = amount;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public PaymentAllocationId Id { get; private set; }

    public PaymentId PaymentId { get; private set; }

    public ChargeId ChargeId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<PaymentAllocation> Create(
        PaymentAllocationId id,
        PaymentId paymentId,
        ChargeId chargeId,
        decimal amount,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(paymentId);
        ArgumentNullException.ThrowIfNull(chargeId);

        if (amount <= 0)
        {
            return Result<PaymentAllocation>.Failure(PaymentErrors.InvalidAmount);
        }

        return Result<PaymentAllocation>.Success(
            new PaymentAllocation(id, paymentId, chargeId, amount, createdAt));
    }
}
