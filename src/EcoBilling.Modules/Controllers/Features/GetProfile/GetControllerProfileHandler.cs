using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.GetProfile;

public sealed class GetControllerProfileHandler(IControllerRepository controllerRepository)
{
    public async Task<Result<ControllerProfile>> Handle(
        GetControllerProfileQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.UserId);

        var controller = await controllerRepository.GetByUserIdAsync(
            query.UserId,
            cancellationToken);

        if (controller is null)
        {
            return Result<ControllerProfile>.Failure(ControllerErrors.NotFound);
        }

        return Result<ControllerProfile>.Success(
            new ControllerProfile(
                controller.Id.Value,
                controller.UserId.Value,
                controller.FullName,
                controller.CreatedAt));
    }
}
