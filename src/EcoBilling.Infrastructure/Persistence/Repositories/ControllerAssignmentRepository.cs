using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ControllerAssignmentRepository(EcoBillingDbContext dbContext)
    : IControllerAssignmentRepository
{
    private const long AssignmentLockId = 2_970_412_884_215_319_241;

    public async Task<ControllerAssignmentPersistenceOutcome> AssignAsync(
        ControllerAssignment assignment,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({AssignmentLockId})",
            cancellationToken);

        var controllerExists = await dbContext.Controllers
            .AsNoTracking()
            .AnyAsync(
                controller => controller.Id == assignment.ControllerId,
                cancellationToken);

        if (!controllerExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return ControllerAssignmentPersistenceOutcome.ControllerNotFound;
        }

        var addressExists = await dbContext.Addresses
            .AsNoTracking()
            .AnyAsync(
                address => address.Id == assignment.AddressId,
                cancellationToken);

        if (!addressExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return ControllerAssignmentPersistenceOutcome.AddressNotFound;
        }

        var alreadyAssigned = await dbContext.ControllerAssignments
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.ControllerId == assignment.ControllerId &&
                    existing.AddressId == assignment.AddressId,
                cancellationToken);

        if (alreadyAssigned)
        {
            await transaction.CommitAsync(cancellationToken);
            return ControllerAssignmentPersistenceOutcome.AlreadyAssigned;
        }

        dbContext.ControllerAssignments.Add(assignment);
        dbContext.AuditLogs.Add(
            CreateAssignedAudit(assignment, actorId, correlationId));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ControllerAssignmentPersistenceOutcome.Assigned;
    }

    public async Task<ControllerAssignmentRemovalOutcome> RemoveAsync(
        ControllerId controllerId,
        ControllerAssignmentId assignmentId,
        string actorId,
        string correlationId,
        DateTimeOffset removedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controllerId);
        ArgumentNullException.ThrowIfNull(assignmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({AssignmentLockId})",
            cancellationToken);

        var assignment = await dbContext.ControllerAssignments
            .SingleOrDefaultAsync(
                existing =>
                    existing.Id == assignmentId &&
                    existing.ControllerId == controllerId,
                cancellationToken);

        if (assignment is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return ControllerAssignmentRemovalOutcome.NotFound;
        }

        dbContext.ControllerAssignments.Remove(assignment);
        dbContext.AuditLogs.Add(
            CreateRemovedAudit(
                assignment,
                actorId,
                correlationId,
                removedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ControllerAssignmentRemovalOutcome.Removed;
    }

    public async Task<IReadOnlyList<ControllerAssignmentDetails>> ListByControllerIdAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controllerId);

        return await (
            from assignment in dbContext.ControllerAssignments.AsNoTracking()
            join address in dbContext.Addresses.AsNoTracking()
                on assignment.AddressId equals address.Id
            where assignment.ControllerId == controllerId
            orderby address.SearchText, assignment.CreatedAt
            select new ControllerAssignmentDetails(
                assignment.Id.Value,
                assignment.ControllerId.Value,
                assignment.AddressId.Value,
                address.Locality,
                address.Street,
                address.House,
                address.Building,
                address.Apartment,
                assignment.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static AuditLog CreateAssignedAudit(
        ControllerAssignment assignment,
        string actorId,
        string correlationId) =>
        new(
            Guid.NewGuid(),
            "User",
            actorId,
            "controllers.assignment.created",
            "ControllerAssignment",
            assignment.Id.Value.ToString("D"),
            beforeData: null,
            JsonSerializer.Serialize(
                new
                {
                    assignmentId = assignment.Id.Value,
                    controllerId = assignment.ControllerId.Value,
                    addressId = assignment.AddressId.Value
                }),
            correlationId,
            assignment.CreatedAt);

    private static AuditLog CreateRemovedAudit(
        ControllerAssignment assignment,
        string actorId,
        string correlationId,
        DateTimeOffset removedAt) =>
        new(
            Guid.NewGuid(),
            "User",
            actorId,
            "controllers.assignment.removed",
            "ControllerAssignment",
            assignment.Id.Value.ToString("D"),
            JsonSerializer.Serialize(
                new
                {
                    assignmentId = assignment.Id.Value,
                    controllerId = assignment.ControllerId.Value,
                    addressId = assignment.AddressId.Value
                }),
            afterData: null,
            correlationId,
            removedAt);
}
