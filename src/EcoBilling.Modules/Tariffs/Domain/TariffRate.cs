using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Domain;

public sealed record TariffRate
{
    private TariffRate(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Result<TariffRate> Create(decimal value)
    {
        if (value < 0)
        {
            return Result<TariffRate>.Failure(TariffErrors.InvalidRate);
        }

        return Result<TariffRate>.Success(new TariffRate(value));
    }

    public override string ToString() => Value.ToString();
}
