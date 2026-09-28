namespace EcoBilling.Modules.Controllers.Domain;

public sealed record ControllerCreationOperationId
{
    public ControllerCreationOperationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A controller creation operation identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
