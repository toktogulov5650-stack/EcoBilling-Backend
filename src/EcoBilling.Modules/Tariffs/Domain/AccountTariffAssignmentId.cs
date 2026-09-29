namespace EcoBilling.Modules.Tariffs.Domain;

public sealed record AccountTariffAssignmentId
{
    public AccountTariffAssignmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "An account tariff assignment identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
