using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Domain;

public sealed class TariffVersion
{
    private TariffVersion()
    {
        Id = null!;
        TariffId = null!;
        Rate = null!;
    }

    private TariffVersion(
        TariffVersionId id,
        TariffId tariffId,
        TariffRate rate,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        DateTimeOffset createdAt)
    {
        Id = id;
        TariffId = tariffId;
        Rate = rate;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CreatedAt = createdAt;
    }

    public TariffVersionId Id { get; private set; }

    public TariffId TariffId { get; private set; }

    public TariffRate Rate { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<TariffVersion> Create(
        TariffVersionId id,
        TariffId tariffId,
        decimal rate,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(tariffId);

        var tariffRate = TariffRate.Create(rate);
        if (tariffRate.IsFailure)
        {
            return Result<TariffVersion>.Failure(tariffRate.Error);
        }

        if (effectiveTo is not null && effectiveTo.Value <= effectiveFrom)
        {
            return Result<TariffVersion>.Failure(TariffErrors.InvalidEffectivePeriod);
        }

        return Result<TariffVersion>.Success(
            new TariffVersion(
                id,
                tariffId,
                tariffRate.Value,
                effectiveFrom,
                effectiveTo,
                createdAt.ToUniversalTime()));
    }
}
