using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.GetWorklist;

public sealed class GetControllerWorklistHandler
{
    private readonly IControllerRepository controllerRepository;
    private readonly IControllerWorklistReader worklistReader;

    public GetControllerWorklistHandler(
        IControllerRepository controllerRepository,
        IControllerWorklistReader worklistReader)
    {
        this.controllerRepository = controllerRepository
            ?? throw new ArgumentNullException(nameof(controllerRepository));
        this.worklistReader = worklistReader
            ?? throw new ArgumentNullException(nameof(worklistReader));
    }

    public async Task<Result<IReadOnlyList<ControllerWorkItem>>> Handle(
        GetControllerWorklistQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.UserId);

        var controller = await controllerRepository.GetByUserIdAsync(
            query.UserId,
            cancellationToken);
        if (controller is null)
        {
            return Result<IReadOnlyList<ControllerWorkItem>>.Failure(
                ControllerErrors.NotFound);
        }

        return Result<IReadOnlyList<ControllerWorkItem>>.Success(
            await worklistReader.ReadAsync(controller.Id, cancellationToken));
    }
}
