namespace EcoBilling.Modules.Tariffs.Domain;

public sealed record TariffId
{
    public TariffId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A tariff identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
