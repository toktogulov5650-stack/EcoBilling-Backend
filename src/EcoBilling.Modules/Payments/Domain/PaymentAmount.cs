using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Domain;

public sealed record PaymentAmount
{
    private PaymentAmount(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Result<PaymentAmount> Create(decimal value)
    {
        if (value <= 0)
        {
            return Result<PaymentAmount>.Failure(PaymentErrors.InvalidAmount);
        }

        return Result<PaymentAmount>.Success(new PaymentAmount(value));
    }

    public override string ToString() => Value.ToString();
}
