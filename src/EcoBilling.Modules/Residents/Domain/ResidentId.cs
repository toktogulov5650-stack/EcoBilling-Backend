namespace EcoBilling.Modules.Residents.Domain;

public sealed record ResidentId
{
    public ResidentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A resident identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
