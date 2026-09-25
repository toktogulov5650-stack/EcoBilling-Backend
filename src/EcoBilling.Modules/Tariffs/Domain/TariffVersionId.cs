namespace EcoBilling.Modules.Tariffs.Domain;

public sealed record TariffVersionId
{
    public TariffVersionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A tariff version identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
