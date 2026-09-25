using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(EcoBillingDbContext dbContext)
    : IAccountRepository
{
    public Task<Account?> GetByNumberAsync(
        AccountNumber accountNumber,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountNumber);

        return dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                account => account.Number == accountNumber,
                cancellationToken);
    }
}
