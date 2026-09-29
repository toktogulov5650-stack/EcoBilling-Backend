using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class ControllerDirectoryReader(EcoBillingDbContext dbContext)
    : IControllerDirectoryReader
{
    public async Task<IReadOnlyList<ControllerDirectoryEntry>> ListAsync(
        CancellationToken cancellationToken)
    {
        var controllers = await dbContext.Controllers
            .AsNoTracking()
            .OrderBy(controller => controller.FullName)
            .ToListAsync(cancellationToken);

        var result = new List<ControllerDirectoryEntry>(controllers.Count);
        foreach (var controller in controllers)
        {
            var user = await dbContext.UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account => account.Id == controller.UserId,
                    cancellationToken);

            var assignmentCount = await dbContext.ControllerAssignments
                .AsNoTracking()
                .CountAsync(
                    assignment => assignment.ControllerId == controller.Id,
                    cancellationToken);

            result.Add(
                new ControllerDirectoryEntry(
                    controller.Id.Value,
                    controller.UserId.Value,
                    controller.FullName,
                    user.LoginIdentity.NormalizedValue,
                    assignmentCount,
                    controller.CreatedAt));
        }

        return result;
    }

    public async Task<ControllerDirectoryEntry?> GetByIdAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controllerId);

        var controller = await dbContext.Controllers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == controllerId,
                cancellationToken);
        if (controller is null)
        {
            return null;
        }

        var user = await dbContext.UserAccounts
            .AsNoTracking()
            .SingleAsync(
                account => account.Id == controller.UserId,
                cancellationToken);

        var assignmentCount = await dbContext.ControllerAssignments
            .AsNoTracking()
            .CountAsync(
                assignment => assignment.ControllerId == controller.Id,
                cancellationToken);

        return new ControllerDirectoryEntry(
            controller.Id.Value,
            controller.UserId.Value,
            controller.FullName,
            user.LoginIdentity.NormalizedValue,
            assignmentCount,
            controller.CreatedAt);
    }
}
