namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentDirectoryEntry(
    Guid ResidentId,
    Guid UserId,
    string FullName,
    Guid AccountId,
    string AccountNumber,
    Guid AddressId,
    string Locality,
    string Street,
    string House,
    string? Building,
    string? Apartment,
    DateTimeOffset CreatedAt);
