namespace EcoBilling.Modules.Meters.Domain;

public sealed record MeterId
{
    public MeterId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A meter identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
