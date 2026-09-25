namespace EcoBilling.Modules.Accounts.Domain;

public sealed record AccountId
{
    public AccountId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An account identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
