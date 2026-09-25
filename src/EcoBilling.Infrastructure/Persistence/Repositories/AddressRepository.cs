using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class AddressRepository(EcoBillingDbContext dbContext)
    : IAddressRepository
{
    public Task<Address?> GetByIdAsync(
        AddressId addressId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(addressId);

        return dbContext.Addresses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                address => address.Id == addressId,
                cancellationToken);
    }
}
