using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.RemoveAssignment;

public sealed class RemoveAssignmentHandler
{
    private readonly IControllerAssignmentRepository repository;
    private readonly TimeProvider timeProvider;

    public RemoveAssignmentHandler(
        IControllerAssignmentRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result> Handle(
        RemoveAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.ControllerId);
        ArgumentNullException.ThrowIfNull(command.AssignmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var outcome = await repository.RemoveAsync(
            command.ControllerId,
            command.AssignmentId,
            command.ActorId,
            command.CorrelationId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return outcome switch
        {
            ControllerAssignmentRemovalOutcome.Removed => Result.Success(),
            ControllerAssignmentRemovalOutcome.NotFound =>
                Result.Failure(ControllerAssignmentErrors.NotFound),
            _ => throw new InvalidOperationException(
                $"Unknown controller assignment removal outcome: {outcome}.")
        };
    }
}
