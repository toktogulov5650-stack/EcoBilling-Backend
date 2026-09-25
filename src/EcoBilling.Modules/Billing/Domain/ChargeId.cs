namespace EcoBilling.Modules.Billing.Domain;

public sealed record ChargeId
{
    public ChargeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A charge identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
