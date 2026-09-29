namespace EcoBilling.Modules.Billing.Features.Abstractions;

public interface IMonthlyBillingBatchProcessor
{
    Task<MonthlyBillingBatchResult> ProcessAsync(
        int year,
        int month,
        CancellationToken cancellationToken);
}

public sealed record MonthlyBillingBatchResult(
    int AccountsScanned,
    int ChargesCreated,
    int Replayed,
    int Skipped);
