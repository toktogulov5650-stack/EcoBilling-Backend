using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.Directory;

public sealed class ListControllersHandler(IControllerDirectoryReader reader)
{
    public async Task<Result<IReadOnlyList<ControllerDirectoryEntry>>> Handle(
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<ControllerDirectoryEntry>>.Success(
            await reader.ListAsync(cancellationToken));
}
