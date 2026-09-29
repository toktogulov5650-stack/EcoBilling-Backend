using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Readings.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class MeterReadingRepository(EcoBillingDbContext dbContext)
    : IMeterReadingRepository
{
    private const long ReadingMutationLockId = 4_516_238_004_711_529_337;

    public Task<MeterReading?> GetByIdAsync(
        MeterReadingId readingId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readingId);

        return dbContext.MeterReadings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reading => reading.Id == readingId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MeterReading>> ListByMeterIdAsync(
        EcoBilling.Modules.Meters.Domain.MeterId meterId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meterId);

        return await dbContext.MeterReadings
            .AsNoTracking()
            .Where(reading => reading.MeterId == meterId)
            .OrderBy(reading => reading.MeasuredAt)
            .ThenBy(reading => reading.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReadingPersistenceResult> AddAsync(
        MeterReading reading,
        ControllerId? controllerId,
        bool actorIsDirector,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({ReadingMutationLockId})",
            cancellationToken);

        var meter = await dbContext.Meters
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.Id == reading.MeterId,
                cancellationToken);
        if (meter is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ReadingPersistenceResult(ReadingPersistenceOutcome.MeterNotFound);
        }

        if (!meter.IsActive)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ReadingPersistenceResult(ReadingPersistenceOutcome.MeterInactive);
        }

        if (!actorIsDirector)
        {
            if (controllerId is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new ReadingPersistenceResult(ReadingPersistenceOutcome.AccessDenied);
            }

            var assigned = await (
                from account in dbContext.Accounts.AsNoTracking()
                join assignment in dbContext.ControllerAssignments.AsNoTracking()
                    on account.AddressId equals assignment.AddressId
                where account.Id == meter.AccountId &&
                      assignment.ControllerId == controllerId
                select assignment.Id)
                .AnyAsync(cancellationToken);

            if (!assigned)
            {
                await transaction.CommitAsync(cancellationToken);
                return new ReadingPersistenceResult(ReadingPersistenceOutcome.AccessDenied);
            }
        }

        if (reading.Source is ReadingSource.Correction)
        {
            if (!actorIsDirector)
            {
                await transaction.CommitAsync(cancellationToken);
                return new ReadingPersistenceResult(
                    ReadingPersistenceOutcome.BackdatedRequiresDirector);
            }

            var superseded = await dbContext.MeterReadings
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    existing =>
                        existing.Id == reading.SupersedesReadingId &&
                        existing.MeterId == reading.MeterId,
                    cancellationToken);
            if (superseded is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new ReadingPersistenceResult(
                    ReadingPersistenceOutcome.SupersededReadingNotFound);
            }

            var alreadyCorrected = await dbContext.MeterReadings
                .AsNoTracking()
                .AnyAsync(
                    existing => existing.SupersedesReadingId == superseded.Id,
                    cancellationToken);
            if (alreadyCorrected)
            {
                await transaction.CommitAsync(cancellationToken);
                return new ReadingPersistenceResult(
                    ReadingPersistenceOutcome.SupersededReadingAlreadyCorrected);
            }
        }
        else
        {
            var latest = await dbContext.MeterReadings
                .AsNoTracking()
                .Where(existing =>
                    existing.MeterId == reading.MeterId &&
                    !dbContext.MeterReadings.Any(
                        correction => correction.SupersedesReadingId == existing.Id))
                .OrderByDescending(existing => existing.MeasuredAt)
                .ThenByDescending(existing => existing.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is not null)
            {
                if (reading.MeasuredAt < latest.MeasuredAt)
                {
                    if (!actorIsDirector)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        return new ReadingPersistenceResult(
                            ReadingPersistenceOutcome.BackdatedRequiresDirector);
                    }

                    if (string.IsNullOrWhiteSpace(reading.CorrectionReason))
                    {
                        await transaction.CommitAsync(cancellationToken);
                        return new ReadingPersistenceResult(
                            ReadingPersistenceOutcome.BackdatedReasonRequired);
                    }
                }

                if (reading.Value.Value < latest.Value.Value)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new ReadingPersistenceResult(
                        ReadingPersistenceOutcome.DecreasedValue);
                }
            }
        }

        dbContext.MeterReadings.Add(reading);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                reading.AuthorUserId?.Value.ToString("D") ?? "system",
                reading.Source is ReadingSource.Correction
                    ? "readings.meter_reading.corrected"
                    : "readings.meter_reading.created",
                "MeterReading",
                reading.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        readingId = reading.Id.Value,
                        meterId = reading.MeterId.Value,
                        value = reading.Value.Value,
                        measuredAt = reading.MeasuredAt,
                        source = reading.Source.ToString(),
                        supersedesReadingId = reading.SupersedesReadingId?.Value,
                        reason = reading.CorrectionReason
                    }),
                correlationId,
                reading.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ReadingPersistenceResult(
            ReadingPersistenceOutcome.Added,
            reading.Id);
    }
}
