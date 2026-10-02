namespace EcoBilling.Modules.Identity.Domain;

public sealed record DirectorPasswordResetOperationId
{
    public DirectorPasswordResetOperationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A director password reset operation identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
}
