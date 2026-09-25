namespace EcoBilling.Modules.Accounts.Contracts;

public sealed record AddressDetails(
    Guid Id,
    string Locality,
    string Street,
    string House,
    string? Building,
    string? Apartment,
    string SearchText);
