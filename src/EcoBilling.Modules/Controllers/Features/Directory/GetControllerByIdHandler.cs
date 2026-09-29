using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.Directory;

public sealed class GetControllerByIdHandler(IControllerDirectoryReader reader)
{
    public async Task<Result<ControllerDirectoryEntry>> Handle(
        ControllerId controllerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controllerId);

        var controller = await reader.GetByIdAsync(controllerId, cancellationToken);
        return controller is null
            ? Result<ControllerDirectoryEntry>.Failure(ControllerErrors.NotFound)
            : Result<ControllerDirectoryEntry>.Success(controller);
    }
}
