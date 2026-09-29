using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Meters.Features.Replace;

public sealed class ReplaceMeterHandler
{
    private readonly IMeterRepository repository;
    private readonly TimeProvider timeProvider;

    public ReplaceMeterHandler(IMeterRepository repository, TimeProvider timeProvider)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<ReplaceMeterResult>> Handle(
        ReplaceMeterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.MeterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var current = await repository.GetByIdAsync(command.MeterId, cancellationToken);
        if (current is null)
        {
            return Result<ReplaceMeterResult>.Failure(MeterErrors.NotFound);
        }

        var replacement = Meter.Create(
            new MeterId(Guid.NewGuid()),
            current.AccountId,
            command.NewSerialNumber,
            command.InstalledAt,
            timeProvider.GetUtcNow(),
            current.Id);
        if (replacement.IsFailure)
        {
            return Result<ReplaceMeterResult>.Failure(replacement.Error);
        }

        var persistence = await repository.ReplaceAsync(
            current.Id,
            replacement.Value,
            command.ActorId,
            command.CorrelationId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return persistence.Outcome switch
        {
            MeterPersistenceOutcome.Replaced =>
                Result<ReplaceMeterResult>.Success(
                    new ReplaceMeterResult(current.Id, persistence.MeterId!)),
            MeterPersistenceOutcome.MeterNotFound =>
                Result<ReplaceMeterResult>.Failure(MeterErrors.NotFound),
            MeterPersistenceOutcome.AlreadyRetired =>
                Result<ReplaceMeterResult>.Failure(MeterErrors.AlreadyRetired),
            MeterPersistenceOutcome.SerialNumberAlreadyExists =>
                Result<ReplaceMeterResult>.Failure(MeterErrors.SerialNumberAlreadyExists),
            MeterPersistenceOutcome.ReplacementAccountMismatch =>
                Result<ReplaceMeterResult>.Failure(MeterErrors.ReplacementMeterMismatch),
            _ => throw new InvalidOperationException(
                $"Unexpected meter replacement outcome: {persistence.Outcome}.")
        };
    }
}
