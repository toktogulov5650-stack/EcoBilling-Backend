using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;

namespace EcoBilling.Modules.Billing.Features.Abstractions;

public interface IBillingCalculationRepository
{
    Task<BillingCalculationPersistenceResult> CalculateAsync(
        AccountId accountId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Charge>> ListByAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}

public sealed record BillingCalculationPersistenceResult(
    BillingCalculationPersistenceOutcome Outcome,
    Charge? Charge = null,
    bool IsReplay = false);

public enum BillingCalculationPersistenceOutcome
{
    Calculated = 0,
    Replayed = 1,
    AccountNotFound = 2,
    MeterNotFound = 3,
    PreviousReadingNotFound = 4,
    CurrentReadingNotFound = 5,
    TariffAssignmentNotFound = 6,
    TariffVersionNotFound = 7,
    InvalidConsumption = 8
}
