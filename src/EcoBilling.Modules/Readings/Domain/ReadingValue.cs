using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Readings.Domain;

public sealed record ReadingValue
{
    private ReadingValue(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Result<ReadingValue> Create(decimal value)
    {
        if (value < 0)
        {
            return Result<ReadingValue>.Failure(MeterReadingErrors.InvalidValue);
        }

        return Result<ReadingValue>.Success(new ReadingValue(value));
    }

    public override string ToString() => Value.ToString();
}
