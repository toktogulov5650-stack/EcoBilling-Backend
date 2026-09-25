using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Domain;

public sealed record PaymentIdempotencyKey
{
    private PaymentIdempotencyKey(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PaymentIdempotencyKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<PaymentIdempotencyKey>.Failure(
                PaymentErrors.InvalidIdempotencyKey);
        }

        return Result<PaymentIdempotencyKey>.Success(
            new PaymentIdempotencyKey(value));
    }

    public override string ToString() => Value;
}
