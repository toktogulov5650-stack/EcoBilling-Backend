using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Domain;

public sealed class Tariff
{
    private Tariff()
    {
        Id = null!;
        Name = null!;
    }

    private Tariff(
        TariffId id,
        TariffName name,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public TariffId Id { get; private set; }

    public TariffName Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Tariff> Create(
        TariffId id,
        string? name,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);

        var tariffName = TariffName.Create(name);
        if (tariffName.IsFailure)
        {
            return Result<Tariff>.Failure(tariffName.Error);
        }

        return Result<Tariff>.Success(
            new Tariff(
                id,
                tariffName.Value,
                createdAt.ToUniversalTime()));
    }
}
