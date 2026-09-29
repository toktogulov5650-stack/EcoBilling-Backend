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
    MultipleMetersRequirePolicy = 4,
    PreviousReadingNotFound = 5,
    CurrentReadingNotFound = 6,
    TariffAssignmentNotFound = 7,
    TariffVersionNotFound = 8,
    InvalidConsumption = 9
}
