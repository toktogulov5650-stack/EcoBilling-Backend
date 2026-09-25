using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentRepository
{
    Task<Resident?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
