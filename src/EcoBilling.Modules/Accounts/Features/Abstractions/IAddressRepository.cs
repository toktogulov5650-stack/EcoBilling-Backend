using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Accounts.Features.Abstractions;

public interface IAddressRepository
{
    Task<Address?> GetByIdAsync(
        AddressId addressId,
        CancellationToken cancellationToken);
}
