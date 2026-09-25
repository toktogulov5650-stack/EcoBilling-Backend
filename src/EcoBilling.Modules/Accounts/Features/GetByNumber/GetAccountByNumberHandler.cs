using EcoBilling.Modules.Accounts.Contracts;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Accounts.Features.GetByNumber;

public sealed class GetAccountByNumberHandler(IAccountRepository accountRepository)
{
    public async Task<Result<AccountDetails>> Handle(
        GetAccountByNumberQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountNumber = AccountNumber.Create(query.AccountNumber);
        if (accountNumber.IsFailure)
        {
            return Result<AccountDetails>.Failure(accountNumber.Error);
        }

        var account = await accountRepository.GetByNumberAsync(
            accountNumber.Value,
            cancellationToken);

        if (account is null)
        {
            return Result<AccountDetails>.Failure(AccountErrors.NotFound);
        }

        return Result<AccountDetails>.Success(
            new AccountDetails(
                account.Id.Value,
                account.ResidentId.Value,
                account.AddressId.Value,
                account.Number.Value,
                account.CreatedAt));
    }
}
