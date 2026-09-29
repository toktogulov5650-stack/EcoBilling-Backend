using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Infrastructure.Outbox;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.Modules.Readings.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class BillingCalculationRepository(
    EcoBillingDbContext dbContext,
    TimeProvider timeProvider)
    : IBillingCalculationRepository
{
    private const long BillingLockId = 6_317_490_221_743_882_107;
    private static readonly TimeZoneInfo BillingTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Bishkek");

    public async Task<BillingCalculationPersistenceResult> CalculateAsync(
        AccountId accountId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({BillingLockId})",
            cancellationToken);

        var existingCharge = await dbContext.Charges
            .AsNoTracking()
            .SingleOrDefaultAsync(
                charge =>
                    charge.AccountId == accountId &&
                    charge.PeriodStart == periodStart &&
                    charge.PeriodEnd == periodEnd,
                cancellationToken);
        if (existingCharge is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.Replayed,
                existingCharge,
                IsReplay: true);
        }

        var accountExists = await dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.Id == accountId, cancellationToken);
        if (!accountExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.AccountNotFound);
        }

        var meter = await dbContext.Meters
            .AsNoTracking()
            .Where(existing => existing.AccountId == accountId && existing.IsActive)
            .OrderByDescending(existing => existing.InstalledAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (meter is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.MeterNotFound);
        }

        var periodStartUtc = ToUtc(periodStart);
        var periodEndUtc = ToUtc(periodEnd);

        var previousReading = await EffectiveReadings(meter.Id)
            .Where(reading => reading.MeasuredAt < periodStartUtc)
            .OrderByDescending(reading => reading.MeasuredAt)
            .ThenByDescending(reading => reading.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (previousReading is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.PreviousReadingNotFound);
        }

        var currentReading = await EffectiveReadings(meter.Id)
            .Where(
                reading =>
                    reading.MeasuredAt >= periodStartUtc &&
                    reading.MeasuredAt < periodEndUtc)
            .OrderByDescending(reading => reading.MeasuredAt)
            .ThenByDescending(reading => reading.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (currentReading is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.CurrentReadingNotFound);
        }

        var consumption = currentReading.Value.Value - previousReading.Value.Value;
        if (consumption < 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.InvalidConsumption);
        }

        var tariffAssignment = await dbContext.AccountTariffAssignments
            .AsNoTracking()
            .Where(
                assignment =>
                    assignment.AccountId == accountId &&
                    assignment.EffectiveFrom <= periodStart &&
                    (assignment.EffectiveTo == null ||
                     periodStart < assignment.EffectiveTo))
            .OrderByDescending(assignment => assignment.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (tariffAssignment is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.TariffAssignmentNotFound);
        }

        var tariffVersion = await dbContext.TariffVersions
            .AsNoTracking()
            .Where(
                version =>
                    version.TariffId == tariffAssignment.TariffId &&
                    version.EffectiveFrom <= periodStart &&
                    (version.EffectiveTo == null ||
                     periodStart < version.EffectiveTo))
            .OrderByDescending(version => version.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (tariffVersion is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BillingCalculationPersistenceResult(
                BillingCalculationPersistenceOutcome.TariffVersionNotFound);
        }

        var amount = Math.Round(
            consumption * tariffVersion.Rate.Value,
            2,
            MidpointRounding.AwayFromZero);

        var chargeResult = Charge.Create(
            new ChargeId(Guid.NewGuid()),
            accountId,
            tariffVersion.Id,
            periodStart,
            periodEnd,
            amount,
            timeProvider.GetUtcNow(),
            previousReading.Id,
            currentReading.Id,
            consumption,
            Charge.V1CalculationVersion,
            Charge.V1Currency);
        if (chargeResult.IsFailure)
        {
            throw new InvalidOperationException(chargeResult.Error.Description);
        }

        var charge = chargeResult.Value;
        dbContext.Charges.Add(charge);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "billing.charge.calculated",
                "Charge",
                charge.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        chargeId = charge.Id.Value,
                        accountId = accountId.Value,
                        previousReadingId = previousReading.Id.Value,
                        currentReadingId = currentReading.Id.Value,
                        consumption,
                        tariffVersionId = tariffVersion.Id.Value,
                        rate = tariffVersion.Rate.Value,
                        amount,
                        currency = charge.Currency,
                        calculationVersion = charge.CalculationVersion
                    }),
                correlationId,
                charge.CreatedAt));
        dbContext.OutboxMessages.Add(
            new OutboxMessage(
                Guid.NewGuid(),
                "billing.charge.calculated.v1",
                JsonSerializer.Serialize(
                    new
                    {
                        chargeId = charge.Id.Value,
                        accountId = accountId.Value,
                        periodStart,
                        periodEnd,
                        amount,
                        currency = charge.Currency
                    }),
                charge.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new BillingCalculationPersistenceResult(
            BillingCalculationPersistenceOutcome.Calculated,
            charge);
    }

    public async Task<IReadOnlyList<Charge>> ListByAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await dbContext.Charges
            .AsNoTracking()
            .Where(charge => charge.AccountId == accountId)
            .OrderByDescending(charge => charge.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<MeterReading> EffectiveReadings(
        EcoBilling.Modules.Meters.Domain.MeterId meterId) =>
        dbContext.MeterReadings
            .AsNoTracking()
            .Where(
                reading =>
                    reading.MeterId == meterId &&
                    !dbContext.MeterReadings.Any(
                        correction =>
                            correction.SupersedesReadingId == reading.Id));

    private static DateTimeOffset ToUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, BillingTimeZone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
