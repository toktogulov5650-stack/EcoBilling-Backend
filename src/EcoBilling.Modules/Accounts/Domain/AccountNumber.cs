using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Accounts.Domain;

public sealed record AccountNumber
{
    private AccountNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<AccountNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<AccountNumber>.Failure(AccountErrors.InvalidAccountNumber);
        }

        return Result<AccountNumber>.Success(
            new AccountNumber(value.Trim().ToUpperInvariant()));
    }

    public override string ToString() => Value;
}
