using EcoBilling.Modules.Billing.Features.Abstractions;

namespace EcoBilling.Worker.Execution;

public sealed class MonthlyBillingWorkerTask(
    IMonthlyBillingBatchProcessor processor,
    TimeProvider timeProvider,
    ILogger<MonthlyBillingWorkerTask> logger)
    : IWorkerTask
{
    public string Name => "monthly-billing";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var billingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bishkek");
        var localNow = TimeZoneInfo.ConvertTime(now, billingTimeZone);
        var previousMonth = new DateOnly(localNow.Year, localNow.Month, 1).AddMonths(-1);

        var result = await processor.ProcessAsync(
            previousMonth.Year,
            previousMonth.Month,
            cancellationToken);

        logger.LogInformation(
            "Monthly billing batch completed for {BillingYear}-{BillingMonth:D2}: scanned {AccountsScanned}, created {ChargesCreated}, replayed {ChargesReplayed}, skipped {ChargesSkipped}",
            previousMonth.Year,
            previousMonth.Month,
            result.AccountsScanned,
            result.ChargesCreated,
            result.Replayed,
            result.Skipped);
    }
}
