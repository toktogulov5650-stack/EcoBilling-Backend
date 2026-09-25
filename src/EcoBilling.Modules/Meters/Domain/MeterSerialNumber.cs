using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Meters.Domain;

public sealed record MeterSerialNumber
{
    private MeterSerialNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<MeterSerialNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<MeterSerialNumber>.Failure(MeterErrors.InvalidSerialNumber);
        }

        return Result<MeterSerialNumber>.Success(
            new MeterSerialNumber(value.Trim().ToUpperInvariant()));
    }

    public override string ToString() => Value;
}
