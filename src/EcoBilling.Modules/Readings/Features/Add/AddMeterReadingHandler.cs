using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Readings.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Readings.Features.Add;

public sealed class AddMeterReadingHandler
{
    private readonly IMeterReadingRepository repository;
    private readonly IControllerRepository controllerRepository;
    private readonly TimeProvider timeProvider;

    public AddMeterReadingHandler(
        IMeterReadingRepository repository,
        IControllerRepository controllerRepository,
        TimeProvider timeProvider)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.controllerRepository = controllerRepository
            ?? throw new ArgumentNullException(nameof(controllerRepository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<MeterReadingId>> Handle(
        AddMeterReadingCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.MeterId);
        ArgumentNullException.ThrowIfNull(command.ActorUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var isDirector = command.ActorRole is UserRole.Director;
        if (!isDirector && command.ActorRole is not UserRole.Controller)
        {
            return Result<MeterReadingId>.Failure(MeterReadingErrors.AccessDenied);
        }

        if (!isDirector && command.Source is ReadingSource.Correction)
        {
            return Result<MeterReadingId>.Failure(
                MeterReadingErrors.BackdatedReadingRequiresDirector);
        }

        EcoBilling.Modules.Controllers.Domain.ControllerId? controllerId = null;
        if (!isDirector)
        {
            var controller = await controllerRepository.GetByUserIdAsync(
                command.ActorUserId,
                cancellationToken);
            if (controller is null)
            {
                return Result<MeterReadingId>.Failure(MeterReadingErrors.AccessDenied);
            }

            controllerId = controller.Id;
        }

        var reading = MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            command.MeterId,
            command.Value,
            command.MeasuredAt,
            timeProvider.GetUtcNow(),
            command.ActorUserId,
            command.Source,
            command.SupersedesReadingId,
            command.Reason);
        if (reading.IsFailure)
        {
            return Result<MeterReadingId>.Failure(reading.Error);
        }

        var outcome = await repository.AddAsync(
            reading.Value,
            controllerId,
            isDirector,
            command.CorrelationId,
            cancellationToken);

        return outcome.Outcome switch
        {
            ReadingPersistenceOutcome.Added =>
                Result<MeterReadingId>.Success(outcome.ReadingId!),
            ReadingPersistenceOutcome.MeterNotFound =>
                Result<MeterReadingId>.Failure(MeterReadingErrors.MeterNotFound),
            ReadingPersistenceOutcome.MeterInactive =>
                Result<MeterReadingId>.Failure(MeterReadingErrors.MeterInactive),
            ReadingPersistenceOutcome.AccessDenied =>
                Result<MeterReadingId>.Failure(MeterReadingErrors.AccessDenied),
            ReadingPersistenceOutcome.DecreasedValue =>
                Result<MeterReadingId>.Failure(MeterReadingErrors.DecreasedValue),
            ReadingPersistenceOutcome.BackdatedRequiresDirector =>
                Result<MeterReadingId>.Failure(
                    MeterReadingErrors.BackdatedReadingRequiresDirector),
            ReadingPersistenceOutcome.BackdatedReasonRequired =>
                Result<MeterReadingId>.Failure(
                    MeterReadingErrors.BackdatedReasonRequired),
            ReadingPersistenceOutcome.SupersededReadingNotFound =>
                Result<MeterReadingId>.Failure(MeterReadingErrors.NotFound),
            ReadingPersistenceOutcome.SupersededReadingAlreadyCorrected =>
                Result<MeterReadingId>.Failure(MeterReadingErrors.AlreadyCorrected),
            _ => throw new InvalidOperationException(
                $"Unexpected reading persistence outcome: {outcome.Outcome}.")
        };
    }
}
