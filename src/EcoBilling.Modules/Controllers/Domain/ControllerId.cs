namespace EcoBilling.Modules.Controllers.Domain;

public sealed record ControllerId
{
    public ControllerId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A controller identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
