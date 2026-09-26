namespace EcoBilling.Modules.Identity.Domain;

public sealed record DirectorProvisioningOperationId
{
    public DirectorProvisioningOperationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A director provisioning operation identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
