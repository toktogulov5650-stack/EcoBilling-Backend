using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.AssignAddress;

public sealed class AssignAddressHandler
{
    private readonly IControllerAssignmentRepository repository;
    private readonly TimeProvider timeProvider;

    public AssignAddressHandler(
        IControllerAssignmentRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<AssignAddressResult>> Handle(
        AssignAddressCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.ControllerId);
        ArgumentNullException.ThrowIfNull(command.AddressId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var assignment = ControllerAssignment.Create(
            new ControllerAssignmentId(Guid.NewGuid()),
            command.ControllerId,
            command.AddressId,
            timeProvider.GetUtcNow());

        if (assignment.IsFailure)
        {
            return Result<AssignAddressResult>.Failure(assignment.Error);
        }

        var outcome = await repository.AssignAsync(
            assignment.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return outcome switch
        {
            ControllerAssignmentPersistenceOutcome.Assigned =>
                Result<AssignAddressResult>.Success(
                    new AssignAddressResult(
                        assignment.Value.Id,
                        assignment.Value.ControllerId,
                        assignment.Value.AddressId,
                        assignment.Value.CreatedAt)),
            ControllerAssignmentPersistenceOutcome.AlreadyAssigned =>
                Result<AssignAddressResult>.Failure(
                    ControllerAssignmentErrors.AlreadyExists),
            ControllerAssignmentPersistenceOutcome.ControllerNotFound =>
                Result<AssignAddressResult>.Failure(
                    ControllerAssignmentErrors.ControllerNotFound),
            ControllerAssignmentPersistenceOutcome.AddressNotFound =>
                Result<AssignAddressResult>.Failure(
                    ControllerAssignmentErrors.AddressNotFound),
            _ => throw new InvalidOperationException(
                $"Unknown controller assignment outcome: {outcome}.")
        };
    }
}
