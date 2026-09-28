namespace EcoBilling.Modules.Identity.Domain;

public sealed record RefreshSessionId
{
    public RefreshSessionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A refresh session identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString("D");
}
