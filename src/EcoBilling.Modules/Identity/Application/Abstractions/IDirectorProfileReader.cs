using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IDirectorProfileReader
{
    Task<DirectorProfileDetails?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
