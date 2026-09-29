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

        var rows = await (
            from assignment in dbContext.ControllerAssignments.AsNoTracking()
            join address in dbContext.Addresses.AsNoTracking()
                on assignment.AddressId equals address.Id
            join account in dbContext.Accounts.AsNoTracking()
                on address.Id equals account.AddressId
            join resident in dbContext.Residents.AsNoTracking()
                on account.ResidentId equals resident.Id
            where assignment.ControllerId == controllerId
            orderby address.SearchText, account.Number
            select new
            {
                AssignmentId = assignment.Id.Value,
                AddressId = address.Id.Value,
                AccountId = account.Id.Value,
                AccountNumber = account.Number.Value,
                ResidentId = resident.Id.Value,
                resident.FullName,
                address.Locality,
                address.Street,
                address.House,
                address.Building,
                address.Apartment
            })
            .ToListAsync(cancellationToken);

        var result = new List<ControllerWorkItem>(rows.Count);
        foreach (var row in rows)
        {
            var accountId = new EcoBilling.Modules.Accounts.Domain.AccountId(row.AccountId);
            var meters = await dbContext.Meters
                .AsNoTracking()
                .Where(meter => meter.AccountId == accountId)
                .OrderByDescending(meter => meter.IsActive)
                .ThenByDescending(meter => meter.InstalledAt)
                .Select(
                    meter => new ControllerWorkMeter(
                        meter.Id.Value,
                        meter.SerialNumber.Value,
                        meter.IsActive,
                        meter.InstalledAt))
                .ToListAsync(cancellationToken);

            result.Add(
                new ControllerWorkItem(
                    row.AssignmentId,
                    row.AddressId,
                    row.AccountId,
                    row.AccountNumber,
                    row.ResidentId,
                    row.FullName,
                    row.Locality,
                    row.Street,
                    row.House,
                    row.Building,
                    row.Apartment,
                    meters));
        }

        return result;
    }
}
