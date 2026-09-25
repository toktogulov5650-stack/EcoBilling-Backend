namespace EcoBilling.Modules.Accounts.Domain;

public sealed record AddressId
{
    public AddressId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An address identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
