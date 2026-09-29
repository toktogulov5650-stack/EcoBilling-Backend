using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Tariffs.Domain;

public static class AccountTariffAssignmentErrors
{
    public static Error InvalidEffectivePeriod { get; } = new(
        "tariff.assignment.invalid_effective_period",
        "The account tariff assignment effective period is invalid.",
        ErrorType.Validation);

    public static Error AccountNotFound { get; } = new(
        "tariff.assignment.account_not_found",
        "The account was not found.",
        ErrorType.NotFound);

    public static Error TariffNotFound { get; } = new(
        "tariff.assignment.tariff_not_found",
        "The tariff was not found.",
        ErrorType.NotFound);

    public static Error Overlap { get; } = new(
        "tariff.assignment.overlap",
        "The account already has a tariff assignment for the requested period.",
        ErrorType.Conflict);

    public static Error NotFound { get; } = new(
        "tariff.assignment.not_found",
        "No tariff assignment was found for the account and date.",
        ErrorType.NotFound);
}
