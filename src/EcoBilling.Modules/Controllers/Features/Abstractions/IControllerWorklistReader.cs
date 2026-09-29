using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public interface IControllerWorklistReader
{
    Task<IReadOnlyList<ControllerWorkItem>> ReadAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken);
}
