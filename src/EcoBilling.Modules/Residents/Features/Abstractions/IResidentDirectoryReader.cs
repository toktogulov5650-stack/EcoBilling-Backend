using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentDirectoryReader
{
    Task<IReadOnlyList<ResidentDirectoryEntry>> ListAsync(
        CancellationToken cancellationToken);

    Task<ResidentDirectoryEntry?> GetByIdAsync(
        ResidentId residentId,
        CancellationToken cancellationToken);
}
