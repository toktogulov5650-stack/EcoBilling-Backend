using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Accounts.Features.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByNumberAsync(
        AccountNumber accountNumber,
        CancellationToken cancellationToken);
}
