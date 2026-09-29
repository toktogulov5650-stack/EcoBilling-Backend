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

    public async Task<ControllerAssignmentPersistenceResult> AssignAsync(
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
            return new ControllerAssignmentPersistenceResult(
                ControllerAssignmentPersistenceOutcome.ControllerNotFound);
        }

        var addressExists = await dbContext.Addresses
            .AsNoTracking()
            .AnyAsync(
                address => address.Id == assignment.AddressId,
                cancellationToken);

        if (!addressExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ControllerAssignmentPersistenceResult(
                ControllerAssignmentPersistenceOutcome.AddressNotFound);
        }

        var existingAssignment = await dbContext.ControllerAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing =>
                    existing.ControllerId == assignment.ControllerId &&
                    existing.AddressId == assignment.AddressId,
                cancellationToken);

        if (existingAssignment is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ControllerAssignmentPersistenceResult(
                ControllerAssignmentPersistenceOutcome.Replayed,
                existingAssignment.Id,
                existingAssignment.CreatedAt);
        }

        dbContext.ControllerAssignments.Add(assignment);
        dbContext.AuditLogs.Add(
            CreateAssignedAudit(assignment, actorId, correlationId));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ControllerAssignmentPersistenceResult(
            ControllerAssignmentPersistenceOutcome.Assigned,
            assignment.Id,
            assignment.CreatedAt);
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

        var assignments = await dbContext.ControllerAssignments
            .AsNoTracking()
            .Where(assignment => assignment.ControllerId == controllerId)
            .OrderBy(assignment => assignment.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<ControllerAssignmentDetails>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var address = await dbContext.Addresses
                .AsNoTracking()
                .SingleAsync(
                    candidate => candidate.Id == assignment.AddressId,
                    cancellationToken);

            result.Add(
                new ControllerAssignmentDetails(
                    assignment.Id.Value,
                    assignment.ControllerId.Value,
                    assignment.AddressId.Value,
                    address.Locality,
                    address.Street,
                    address.House,
                    address.Building,
                    address.Apartment,
                    assignment.CreatedAt));
        }

        return result
            .OrderBy(item => item.Locality, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Street, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.House, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.CreatedAt)
            .ToArray();
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
