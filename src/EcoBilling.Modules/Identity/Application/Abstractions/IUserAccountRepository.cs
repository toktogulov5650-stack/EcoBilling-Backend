using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByLoginAsync(
        LoginIdentity loginIdentity,
        CancellationToken cancellationToken);
}
