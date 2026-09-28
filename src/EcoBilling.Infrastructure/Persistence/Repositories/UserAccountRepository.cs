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
            .SingleOrDefaultAsync(
                userAccount =>
                    userAccount.LoginIdentity.Type == loginIdentity.Type &&
                    userAccount.LoginIdentity.NormalizedValue == loginIdentity.NormalizedValue,
                cancellationToken);
    }

    public async Task SaveAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userAccount);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
