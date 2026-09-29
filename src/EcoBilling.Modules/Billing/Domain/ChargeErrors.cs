using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Billing.Domain;

public static class ChargeErrors
{
    public static Error InvalidBillingPeriod { get; } = new(
        "billing.invalid_period",
        "The billing period is invalid.",
        ErrorType.Validation);

    public static Error InvalidAmount { get; } = new(
        "billing.invalid_amount",
        "The calculated charge amount is invalid.",
        ErrorType.Validation);

    public static Error InvalidCalculationVersion { get; } = new(
        "billing.invalid_calculation_version",
        "The calculation version is invalid.",
        ErrorType.Validation);

    public static Error InvalidCurrency { get; } = new(
        "billing.invalid_currency",
        "The billing currency is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "billing.not_found",
        "The charge was not found.",
        ErrorType.NotFound);

    public static Error AccountNotFound { get; } = new(
        "billing.account_not_found",
        "The account was not found.",
        ErrorType.NotFound);

    public static Error MeterNotFound { get; } = new(
        "billing.meter_not_found",
        "No active meter is available for the account.",
        ErrorType.NotFound);

    public static Error PreviousReadingNotFound { get; } = new(
        "billing.previous_reading_not_found",
        "A previous accepted reading is required to calculate the charge.",
        ErrorType.Validation);

    public static Error CurrentReadingNotFound { get; } = new(
        "billing.current_reading_not_found",
        "A current accepted reading is required to calculate the charge.",
        ErrorType.Validation);

    public static Error TariffAssignmentNotFound { get; } = new(
        "billing.tariff_assignment_not_found",
        "No tariff is assigned to the account for the billing period.",
        ErrorType.Validation);

    public static Error TariffVersionNotFound { get; } = new(
        "billing.tariff_version_not_found",
        "No tariff version is effective for the billing period.",
        ErrorType.Validation);

    public static Error AlreadyCalculated { get; } = new(
        "billing.already_calculated",
        "The account already has a charge for the requested billing period.",
        ErrorType.Conflict);
}
