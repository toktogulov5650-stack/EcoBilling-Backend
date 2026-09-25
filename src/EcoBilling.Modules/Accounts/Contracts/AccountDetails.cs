namespace EcoBilling.Modules.Accounts.Contracts;

public sealed record AccountDetails(
    Guid Id,
    Guid ResidentId,
    Guid AddressId,
    string AccountNumber,
    DateTimeOffset CreatedAt);
