using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Domain;

public sealed record TariffName
{
    private TariffName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<TariffName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<TariffName>.Failure(TariffErrors.InvalidName);
        }

        var normalized = string.Join(
            ' ',
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return Result<TariffName>.Success(new TariffName(normalized));
    }

    public override string ToString() => Value;
}
