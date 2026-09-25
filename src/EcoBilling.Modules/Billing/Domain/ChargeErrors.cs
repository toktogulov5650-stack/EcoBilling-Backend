using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Billing.Domain;

public static class ChargeErrors
{
    public static Error InvalidBillingPeriod { get; } = new(
        "billing.invalid_period",
        "The billing period is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "billing.not_found",
        "The charge was not found.",
        ErrorType.NotFound);
}
