using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IUserAccountRepository
{
    Task<UserAccount?> FindByLoginAsync(
        LoginType loginType,
        string normalizedLogin,
        CancellationToken cancellationToken);
}
