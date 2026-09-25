using EcoBilling.Modules.Residents.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Accounts.Domain;

public sealed class Account
{
    private Account()
    {
        Id = null!;
        ResidentId = null!;
        AddressId = null!;
        Number = null!;
    }

    private Account(
        AccountId id,
        ResidentId residentId,
        AddressId addressId,
        AccountNumber number,
        DateTimeOffset createdAt)
    {
        Id = id;
        ResidentId = residentId;
        AddressId = addressId;
        Number = number;
        CreatedAt = createdAt;
    }

    public AccountId Id { get; private set; }

    public ResidentId ResidentId { get; private set; }

    public AddressId AddressId { get; private set; }

    public AccountNumber Number { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Account> Create(
        AccountId id,
        ResidentId residentId,
        AddressId addressId,
        string? accountNumber,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(residentId);
        ArgumentNullException.ThrowIfNull(addressId);

        var number = AccountNumber.Create(accountNumber);
        if (number.IsFailure)
        {
            return Result<Account>.Failure(number.Error);
        }

        return Result<Account>.Success(
            new Account(
                id,
                residentId,
                addressId,
                number.Value,
                createdAt.ToUniversalTime()));
    }
}
