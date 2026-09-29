using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Infrastructure.Outbox;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Payments.Contracts;
using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Payments.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(EcoBillingDbContext dbContext)
    : IPaymentRepository
{
    private const long PaymentMutationLockId = 8_207_390_554_116_287_063;

    public Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paymentId);

        return dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                payment => payment.Id == paymentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> ListByAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.AccountId == accountId)
            .OrderByDescending(payment => payment.PaidAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountFinancialSummary?> GetFinancialSummaryAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        if (!await dbContext.Accounts.AsNoTracking().AnyAsync(
                account => account.Id == accountId,
                cancellationToken))
        {
            return null;
        }

        var totalCharges = await dbContext.Charges
            .AsNoTracking()
            .Where(charge => charge.AccountId == accountId)
            .SumAsync(charge => (decimal?)charge.Amount, cancellationToken) ?? 0m;

        var totalPayments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.AccountId == accountId)
            .SumAsync(payment => (decimal?)payment.Amount.Value, cancellationToken) ?? 0m;

        var totalAllocated = await (
            from allocation in dbContext.PaymentAllocations.AsNoTracking()
            join payment in dbContext.Payments.AsNoTracking()
                on allocation.PaymentId equals payment.Id
            where payment.AccountId == accountId
            select (decimal?)allocation.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var outstandingDebt = Math.Max(0m, totalCharges - totalAllocated);
        var overpayment = Math.Max(0m, totalPayments - totalAllocated);

        return new AccountFinancialSummary(
            accountId.Value,
            totalCharges,
            totalAllocated,
            outstandingDebt,
            overpayment,
            "KGS");
    }

    public async Task<PaymentRegistrationPersistenceResult> RegisterAsync(
        Payment payment,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({PaymentMutationLockId})",
            cancellationToken);

        var existing = await dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.IdempotencyKey == payment.IdempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.AccountId != payment.AccountId ||
                existing.Amount != payment.Amount)
            {
                await transaction.CommitAsync(cancellationToken);
                return new PaymentRegistrationPersistenceResult(
                    PaymentRegistrationPersistenceOutcome.IdempotencyConflict);
            }

            var existingAllocated = await dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(allocation => allocation.PaymentId == existing.Id)
                .SumAsync(
                    allocation => (decimal?)allocation.Amount,
                    cancellationToken) ?? 0m;

            await transaction.CommitAsync(cancellationToken);
            return new PaymentRegistrationPersistenceResult(
                PaymentRegistrationPersistenceOutcome.Replayed,
                existing,
                Math.Max(0m, existing.Amount.Value - existingAllocated),
                IsReplay: true);
        }

        var accountExists = await dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.Id == payment.AccountId, cancellationToken);
        if (!accountExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new PaymentRegistrationPersistenceResult(
                PaymentRegistrationPersistenceOutcome.AccountNotFound);
        }

        var charges = await dbContext.Charges
            .AsNoTracking()
            .Where(charge => charge.AccountId == payment.AccountId)
            .OrderBy(charge => charge.PeriodStart)
            .ThenBy(charge => charge.CreatedAt)
            .ToListAsync(cancellationToken);

        var remaining = payment.Amount.Value;
        var allocations = new List<PaymentAllocation>();

        foreach (var charge in charges)
        {
            if (remaining <= 0)
            {
                break;
            }

            var previouslyAllocated = await dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(allocation => allocation.ChargeId == charge.Id)
                .SumAsync(
                    allocation => (decimal?)allocation.Amount,
                    cancellationToken) ?? 0m;

            var outstanding = charge.Amount - previouslyAllocated;
            if (outstanding <= 0)
            {
                continue;
            }

            var allocatedAmount = Math.Min(remaining, outstanding);
            var allocation = PaymentAllocation.Create(
                new PaymentAllocationId(Guid.NewGuid()),
                payment.Id,
                charge.Id,
                allocatedAmount,
                payment.CreatedAt);
            if (allocation.IsFailure)
            {
                throw new InvalidOperationException(allocation.Error.Description);
            }

            allocations.Add(allocation.Value);
            remaining -= allocatedAmount;
        }

        dbContext.Payments.Add(payment);
        dbContext.PaymentAllocations.AddRange(allocations);
        dbContext.AuditLogs.Add(
            new AuditLog(
                Guid.NewGuid(),
                "User",
                actorId,
                "payments.payment.registered",
                "Payment",
                payment.Id.Value.ToString("D"),
                beforeData: null,
                JsonSerializer.Serialize(
                    new
                    {
                        paymentId = payment.Id.Value,
                        accountId = payment.AccountId.Value,
                        amount = payment.Amount.Value,
                        paidAt = payment.PaidAt,
                        allocatedAmount = payment.Amount.Value - remaining,
                        overpayment = remaining
                    }),
                correlationId,
                payment.CreatedAt));
        dbContext.OutboxMessages.Add(
            new OutboxMessage(
                Guid.NewGuid(),
                "payments.payment.registered.v1",
                JsonSerializer.Serialize(
                    new
                    {
                        paymentId = payment.Id.Value,
                        accountId = payment.AccountId.Value,
                        amount = payment.Amount.Value,
                        overpayment = remaining
                    }),
                payment.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PaymentRegistrationPersistenceResult(
            PaymentRegistrationPersistenceOutcome.Registered,
            payment,
            remaining);
    }
}
