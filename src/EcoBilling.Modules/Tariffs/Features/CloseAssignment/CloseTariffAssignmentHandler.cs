using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.CloseAssignment;

public sealed class CloseTariffAssignmentHandler
{
    private readonly IAccountTariffAssignmentRepository repository;
    private readonly TimeProvider timeProvider;

    public CloseTariffAssignmentHandler(
        IAccountTariffAssignmentRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result> Handle(
        CloseTariffAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AccountId);
        ArgumentNullException.ThrowIfNull(command.AssignmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var outcome = await repository.CloseAsync(
            command.AccountId,
            command.AssignmentId,
            command.EffectiveTo,
            command.ActorId,
            command.CorrelationId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return outcome switch
        {
            AccountTariffAssignmentCloseOutcome.Closed => Result.Success(),
            AccountTariffAssignmentCloseOutcome.NotFound =>
                Result.Failure(AccountTariffAssignmentErrors.NotFound),
            AccountTariffAssignmentCloseOutcome.InvalidEffectivePeriod =>
                Result.Failure(AccountTariffAssignmentErrors.InvalidEffectivePeriod),
            AccountTariffAssignmentCloseOutcome.AlreadyClosed =>
                Result.Failure(AccountTariffAssignmentErrors.AlreadyClosed),
            _ => throw new InvalidOperationException(
                $"Unknown tariff assignment close outcome: {outcome}.")
        };
    }
}
