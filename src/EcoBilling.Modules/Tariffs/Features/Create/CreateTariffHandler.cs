using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.Create;

public sealed class CreateTariffHandler
{
    private readonly ITariffRepository repository;
    private readonly TimeProvider timeProvider;

    public CreateTariffHandler(ITariffRepository repository, TimeProvider timeProvider)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<TariffId>> Handle(
        CreateTariffCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var tariff = Tariff.Create(
            new TariffId(Guid.NewGuid()),
            command.Name,
            timeProvider.GetUtcNow());
        if (tariff.IsFailure)
        {
            return Result<TariffId>.Failure(tariff.Error);
        }

        var persistence = await repository.CreateAsync(
            tariff.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistence.Outcome switch
        {
            TariffPersistenceOutcome.Created =>
                Result<TariffId>.Success(persistence.TariffId!),
            TariffPersistenceOutcome.NameAlreadyExists =>
                Result<TariffId>.Failure(TariffErrors.NameAlreadyExists),
            _ => throw new InvalidOperationException(
                $"Unexpected tariff persistence outcome: {persistence.Outcome}.")
        };
    }
}
