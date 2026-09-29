using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Meters.Features.Create;

public sealed class CreateMeterHandler
{
    private readonly IMeterRepository repository;
    private readonly TimeProvider timeProvider;

    public CreateMeterHandler(IMeterRepository repository, TimeProvider timeProvider)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<MeterId>> Handle(
        CreateMeterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var meter = Meter.Create(
            new MeterId(Guid.NewGuid()),
            command.AccountId,
            command.SerialNumber,
            command.InstalledAt,
            timeProvider.GetUtcNow());
        if (meter.IsFailure)
        {
            return Result<MeterId>.Failure(meter.Error);
        }

        var persistence = await repository.CreateAsync(
            meter.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistence.Outcome switch
        {
            MeterPersistenceOutcome.Created =>
                Result<MeterId>.Success(persistence.MeterId!),
            MeterPersistenceOutcome.AccountNotFound =>
                Result<MeterId>.Failure(MeterErrors.AccountNotFound),
            MeterPersistenceOutcome.SerialNumberAlreadyExists =>
                Result<MeterId>.Failure(MeterErrors.SerialNumberAlreadyExists),
            _ => throw new InvalidOperationException(
                $"Unexpected meter persistence outcome: {persistence.Outcome}.")
        };
    }
}
