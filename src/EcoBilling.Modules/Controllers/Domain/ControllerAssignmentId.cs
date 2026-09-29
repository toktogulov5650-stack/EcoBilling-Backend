namespace EcoBilling.Modules.Controllers.Domain;

public sealed record ControllerAssignmentId
{
    public ControllerAssignmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A controller assignment identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
