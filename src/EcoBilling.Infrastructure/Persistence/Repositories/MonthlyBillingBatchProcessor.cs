using EcoBilling.Modules.Billing.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class MonthlyBillingBatchProcessor(
    EcoBillingDbContext dbContext,
    IBillingCalculationRepository billingRepository)
    : IMonthlyBillingBatchProcessor
{
    public async Task<MonthlyBillingBatchResult> ProcessAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        if (year < 2000 || year > 2200 || month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var accountIds = await dbContext.Accounts
            .AsNoTracking()
            .Select(account => account.Id)
            .ToListAsync(cancellationToken);

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        var created = 0;
        var replayed = 0;
        var skipped = 0;

        foreach (var accountId in accountIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await billingRepository.CalculateAsync(
                accountId,
                start,
                end,
                "worker",
                $"worker-billing-{year:D4}-{month:D2}-{accountId.Value:D}",
                cancellationToken);

            switch (result.Outcome)
            {
                case BillingCalculationPersistenceOutcome.Calculated:
                    created++;
                    break;
                case BillingCalculationPersistenceOutcome.Replayed:
                    replayed++;
                    break;
                default:
                    skipped++;
                    break;
            }
        }

        return new MonthlyBillingBatchResult(
            accountIds.Count,
            created,
            replayed,
            skipped);
    }
}
