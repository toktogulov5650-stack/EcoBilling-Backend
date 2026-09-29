using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.Directory;

public sealed class GetResidentByIdHandler(IResidentDirectoryReader reader)
{
    public async Task<Result<ResidentDirectoryEntry>> Handle(
        ResidentId residentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(residentId);

        var resident = await reader.GetByIdAsync(residentId, cancellationToken);
        return resident is null
            ? Result<ResidentDirectoryEntry>.Failure(ResidentErrors.NotFound)
            : Result<ResidentDirectoryEntry>.Success(resident);
    }
}
