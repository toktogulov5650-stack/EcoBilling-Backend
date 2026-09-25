using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Domain;

public sealed class Payment
{
    private Payment()
    {
        Id = null!;
        AccountId = null!;
        Amount = null!;
        IdempotencyKey = null!;
    }

    private Payment(
        PaymentId id,
        AccountId accountId,
        PaymentAmount amount,
        PaymentIdempotencyKey idempotencyKey,
        DateTimeOffset paidAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        Amount = amount;
        IdempotencyKey = idempotencyKey;
        PaidAt = paidAt;
        CreatedAt = createdAt;
    }

    public PaymentId Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public PaymentAmount Amount { get; private set; }

    public PaymentIdempotencyKey IdempotencyKey { get; private set; }

    public DateTimeOffset PaidAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Payment> Create(
        PaymentId id,
        AccountId accountId,
        decimal amount,
        string? idempotencyKey,
        DateTimeOffset paidAt,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(accountId);

        var paymentAmount = PaymentAmount.Create(amount);
        if (paymentAmount.IsFailure)
        {
            return Result<Payment>.Failure(paymentAmount.Error);
        }

        var paymentIdempotencyKey = PaymentIdempotencyKey.Create(idempotencyKey);
        if (paymentIdempotencyKey.IsFailure)
        {
            return Result<Payment>.Failure(paymentIdempotencyKey.Error);
        }

        return Result<Payment>.Success(
            new Payment(
                id,
                accountId,
                paymentAmount.Value,
                paymentIdempotencyKey.Value,
                paidAt.ToUniversalTime(),
                createdAt.ToUniversalTime()));
    }
}
