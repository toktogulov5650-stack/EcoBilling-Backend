using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Accounts.Domain;

public sealed class Address
{
    private Address()
    {
        Id = null!;
        Locality = string.Empty;
        Street = string.Empty;
        House = string.Empty;
        SearchText = string.Empty;
    }

    private Address(
        AddressId id,
        string locality,
        string street,
        string house,
        string? building,
        string? apartment)
    {
        Id = id;
        Locality = locality;
        Street = street;
        House = house;
        Building = building;
        Apartment = apartment;
        SearchText = BuildSearchText(locality, street, house, building, apartment);
    }

    public AddressId Id { get; private set; }

    public string Locality { get; private set; }

    public string Street { get; private set; }

    public string House { get; private set; }

    public string? Building { get; private set; }

    public string? Apartment { get; private set; }

    public string SearchText { get; private set; }

    public static Result<Address> Create(
        AddressId id,
        string? locality,
        string? street,
        string? house,
        string? building,
        string? apartment)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (string.IsNullOrWhiteSpace(locality))
        {
            return Result<Address>.Failure(AddressErrors.InvalidLocality);
        }

        if (string.IsNullOrWhiteSpace(street))
        {
            return Result<Address>.Failure(AddressErrors.InvalidStreet);
        }

        if (string.IsNullOrWhiteSpace(house))
        {
            return Result<Address>.Failure(AddressErrors.InvalidHouse);
        }

        return Result<Address>.Success(
            new Address(
                id,
                Normalize(locality),
                Normalize(street),
                Normalize(house),
                NormalizeOptional(building),
                NormalizeOptional(apartment)));
    }

    private static string BuildSearchText(
        string locality,
        string street,
        string house,
        string? building,
        string? apartment)
    {
        var parts = new[] { locality, street, house, building, apartment }
            .Where(part => part is not null);

        return string.Join(' ', parts).ToUpperInvariant();
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}
