using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.CreateVersion;

public sealed class CreateTariffVersionHandler
{
    private readonly ITariffVersionRepository repository;
    private readonly TimeProvider timeProvider;

    public CreateTariffVersionHandler(
        ITariffVersionRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<TariffVersionId>> Handle(
        CreateTariffVersionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.TariffId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var version = TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            command.TariffId,
            command.Rate,
            command.EffectiveFrom,
            command.EffectiveTo,
            timeProvider.GetUtcNow());
        if (version.IsFailure)
        {
            return Result<TariffVersionId>.Failure(version.Error);
        }

        var persistence = await repository.CreateAsync(
            version.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistence.Outcome switch
        {
            TariffVersionPersistenceOutcome.Created =>
                Result<TariffVersionId>.Success(persistence.TariffVersionId!),
            TariffVersionPersistenceOutcome.TariffNotFound =>
                Result<TariffVersionId>.Failure(TariffErrors.NotFound),
            TariffVersionPersistenceOutcome.OverlappingPeriod =>
                Result<TariffVersionId>.Failure(TariffErrors.OverlappingPeriod),
            _ => throw new InvalidOperationException(
                $"Unexpected tariff version persistence outcome: {persistence.Outcome}.")
        };
    }
}
