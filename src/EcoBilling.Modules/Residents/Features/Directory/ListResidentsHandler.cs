using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.Directory;

public sealed class ListResidentsHandler(IResidentDirectoryReader reader)
{
    public async Task<Result<IReadOnlyList<ResidentDirectoryEntry>>> Handle(
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<ResidentDirectoryEntry>>.Success(
            await reader.ListAsync(cancellationToken));
}
