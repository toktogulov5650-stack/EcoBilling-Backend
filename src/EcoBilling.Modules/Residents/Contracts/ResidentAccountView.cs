namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentAccountView(
    Guid ResidentId,
    string FullName,
    Guid AccountId,
    string AccountNumber,
    Guid AddressId,
    string Locality,
    string Street,
    string House,
    string? Building,
    string? Apartment);
