namespace EcoBilling.Modules.Residents.Domain;

public sealed record ResidentPasswordResetOperationId
{
    public ResidentPasswordResetOperationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A resident password reset operation identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
}
