using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.CloseVersion;

public sealed class CloseTariffVersionHandler
{
    private readonly ITariffVersionRepository repository;
    private readonly TimeProvider timeProvider;

    public CloseTariffVersionHandler(
        ITariffVersionRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result> Handle(
        CloseTariffVersionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.TariffId);
        ArgumentNullException.ThrowIfNull(command.TariffVersionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var outcome = await repository.CloseAsync(
            command.TariffId,
            command.TariffVersionId,
            command.EffectiveTo,
            command.ActorId,
            command.CorrelationId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return outcome switch
        {
            TariffVersionCloseOutcome.Closed => Result.Success(),
            TariffVersionCloseOutcome.NotFound =>
                Result.Failure(TariffErrors.VersionNotFound),
            TariffVersionCloseOutcome.InvalidEffectivePeriod =>
                Result.Failure(TariffErrors.InvalidEffectivePeriod),
            TariffVersionCloseOutcome.AlreadyClosed =>
                Result.Failure(TariffErrors.OverlappingPeriod),
            _ => throw new InvalidOperationException(
                $"Unknown tariff version close outcome: {outcome}.")
        };
    }
}
