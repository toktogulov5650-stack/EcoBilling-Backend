using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.ListAssignments;

public sealed class ListControllerAssignmentsHandler
{
    private readonly IControllerRepository controllerRepository;
    private readonly IControllerAssignmentRepository assignmentRepository;

    public ListControllerAssignmentsHandler(
        IControllerRepository controllerRepository,
        IControllerAssignmentRepository assignmentRepository)
    {
        this.controllerRepository = controllerRepository
            ?? throw new ArgumentNullException(nameof(controllerRepository));
        this.assignmentRepository = assignmentRepository
            ?? throw new ArgumentNullException(nameof(assignmentRepository));
    }

    public async Task<Result<IReadOnlyList<ControllerAssignmentDetails>>> Handle(
        ListControllerAssignmentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.ControllerId);

        var controller = await controllerRepository.GetByIdAsync(
            query.ControllerId,
            cancellationToken);
        if (controller is null)
        {
            return Result<IReadOnlyList<ControllerAssignmentDetails>>.Failure(
                ControllerErrors.NotFound);
        }

        var assignments = await assignmentRepository.ListByControllerIdAsync(
            controller.Id,
            cancellationToken);

        return Result<IReadOnlyList<ControllerAssignmentDetails>>.Success(
            assignments);
    }
}
