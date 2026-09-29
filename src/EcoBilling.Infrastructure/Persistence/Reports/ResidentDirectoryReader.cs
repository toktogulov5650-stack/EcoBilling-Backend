using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class ResidentDirectoryReader(EcoBillingDbContext dbContext)
    : IResidentDirectoryReader
{
    public async Task<IReadOnlyList<ResidentDirectoryEntry>> ListAsync(
        CancellationToken cancellationToken)
    {
        var residents = await dbContext.Residents
            .AsNoTracking()
            .OrderBy(resident => resident.FullName)
            .ToListAsync(cancellationToken);

        var result = new List<ResidentDirectoryEntry>(residents.Count);
        foreach (var resident in residents)
        {
            var account = await dbContext.Accounts
                .AsNoTracking()
                .SingleAsync(
                    candidate => candidate.ResidentId == resident.Id,
                    cancellationToken);
            var address = await dbContext.Addresses
                .AsNoTracking()
                .SingleAsync(
                    candidate => candidate.Id == account.AddressId,
                    cancellationToken);

            result.Add(
                Map(resident, account, address));
        }

        return result;
    }

    public async Task<ResidentDirectoryEntry?> GetByIdAsync(
        ResidentId residentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(residentId);

        var resident = await dbContext.Residents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == residentId,
                cancellationToken);
        if (resident is null)
        {
            return null;
        }

        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.ResidentId == resident.Id,
                cancellationToken);
        var address = await dbContext.Addresses
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == account.AddressId,
                cancellationToken);

        return Map(resident, account, address);
    }

    private static ResidentDirectoryEntry Map(
        Resident resident,
        EcoBilling.Modules.Accounts.Domain.Account account,
        EcoBilling.Modules.Accounts.Domain.Address address) =>
        new(
            resident.Id.Value,
            resident.UserId.Value,
            resident.FullName,
            account.Id.Value,
            account.Number.Value,
            address.Id.Value,
            address.Locality,
            address.Street,
            address.House,
            address.Building,
            address.Apartment,
            resident.CreatedAt);
}
