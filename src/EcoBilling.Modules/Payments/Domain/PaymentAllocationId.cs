namespace EcoBilling.Modules.Payments.Domain;

public sealed record PaymentAllocationId
{
    public PaymentAllocationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A payment allocation identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
