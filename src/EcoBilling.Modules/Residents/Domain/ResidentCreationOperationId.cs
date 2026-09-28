namespace EcoBilling.Modules.Residents.Domain;

public sealed record ResidentCreationOperationId
{
    public ResidentCreationOperationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A resident creation operation identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
