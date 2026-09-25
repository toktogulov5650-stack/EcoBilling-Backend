using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Accounts.Domain;

public static class AccountErrors
{
    public static Error InvalidAccountNumber { get; } = new(
        "account.invalid_account_number",
        "The account number is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "account.not_found",
        "The account was not found.",
        ErrorType.NotFound);
}
