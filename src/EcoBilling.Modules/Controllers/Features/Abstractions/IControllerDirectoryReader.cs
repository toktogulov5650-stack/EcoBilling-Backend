using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public interface IControllerDirectoryReader
{
    Task<IReadOnlyList<ControllerDirectoryEntry>> ListAsync(
        CancellationToken cancellationToken);

    Task<ControllerDirectoryEntry?> GetByIdAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken);
}
