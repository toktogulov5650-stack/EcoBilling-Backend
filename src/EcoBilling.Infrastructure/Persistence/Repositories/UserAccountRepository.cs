using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class UserAccountRepository(EcoBillingDbContext dbContext)
    : IUserAccountRepository
{
    public Task<UserAccount?> GetByLoginAsync(
        LoginIdentity loginIdentity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loginIdentity);

        return dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                userAccount =>
                    userAccount.LoginIdentity.Type == loginIdentity.Type &&
                    userAccount.LoginIdentity.NormalizedValue == loginIdentity.NormalizedValue,
                cancellationToken);
    }
}
