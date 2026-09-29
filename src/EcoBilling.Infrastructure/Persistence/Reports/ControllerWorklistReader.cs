using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class ControllerWorklistReader(EcoBillingDbContext dbContext)
    : IControllerWorklistReader
{
    public async Task<IReadOnlyList<ControllerWorkItem>> ReadAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controllerId);

        var assignments = await dbContext.ControllerAssignments
            .AsNoTracking()
            .Where(assignment => assignment.ControllerId == controllerId)
            .OrderBy(assignment => assignment.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<ControllerWorkItem>();

        foreach (var assignment in assignments)
        {
            var address = await dbContext.Addresses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == assignment.AddressId,
                    cancellationToken);
            if (address is null)
            {
                continue;
            }

            var accounts = await dbContext.Accounts
                .AsNoTracking()
                .Where(account => account.AddressId == address.Id)
                .OrderBy(account => account.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var account in accounts)
            {
                var resident = await dbContext.Residents
                    .AsNoTracking()
                    .SingleAsync(
                        candidate => candidate.Id == account.ResidentId,
                        cancellationToken);

                var meters = await dbContext.Meters
                    .AsNoTracking()
                    .Where(meter => meter.AccountId == account.Id)
                    .OrderByDescending(meter => meter.IsActive)
                    .ThenByDescending(meter => meter.InstalledAt)
                    .ToListAsync(cancellationToken);

                result.Add(
                    new ControllerWorkItem(
                        assignment.Id.Value,
                        address.Id.Value,
                        account.Id.Value,
                        account.Number.Value,
                        resident.Id.Value,
                        resident.FullName,
                        address.Locality,
                        address.Street,
                        address.House,
                        address.Building,
                        address.Apartment,
                        meters.Select(
                                meter => new ControllerWorkMeter(
                                    meter.Id.Value,
                                    meter.SerialNumber.Value,
                                    meter.IsActive,
                                    meter.InstalledAt))
                            .ToArray()));
            }
        }

        return result;
    }
}
