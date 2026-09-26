namespace EcoBilling.Modules.Identity.Domain;

public sealed record DirectorId
{
    public DirectorId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A director identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
