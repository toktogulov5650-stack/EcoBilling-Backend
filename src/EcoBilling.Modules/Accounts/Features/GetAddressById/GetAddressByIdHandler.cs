using EcoBilling.Modules.Accounts.Contracts;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Accounts.Features.GetAddressById;

public sealed class GetAddressByIdHandler(IAddressRepository addressRepository)
{
    public async Task<Result<AddressDetails>> Handle(
        GetAddressByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AddressId);

        var address = await addressRepository.GetByIdAsync(
            query.AddressId,
            cancellationToken);

        if (address is null)
        {
            return Result<AddressDetails>.Failure(AddressErrors.NotFound);
        }

        return Result<AddressDetails>.Success(
            new AddressDetails(
                address.Id.Value,
                address.Locality,
                address.Street,
                address.House,
                address.Building,
                address.Apartment,
                address.SearchText));
    }
}
