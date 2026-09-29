using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Billing.Features.CalculateMonthly;

public sealed record CalculateMonthlyChargeCommand(
    AccountId AccountId,
    int Year,
    int Month,
    string ActorId,
    string CorrelationId);
