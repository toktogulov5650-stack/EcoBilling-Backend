namespace EcoBilling.Modules.Readings.Domain;

public sealed record MeterReadingId
{
    public MeterReadingId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A meter reading identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();
}
