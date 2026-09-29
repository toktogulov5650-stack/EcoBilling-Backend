using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Tariffs.Domain;

public static class TariffErrors
{
    public static Error InvalidName { get; } = new(
        "tariff.invalid_name",
        "The tariff name is invalid.",
        ErrorType.Validation);

    public static Error InvalidRate { get; } = new(
        "tariff.invalid_rate",
        "The tariff rate is invalid.",
        ErrorType.Validation);

    public static Error InvalidEffectivePeriod { get; } = new(
        "tariff.invalid_effective_period",
        "The tariff version effective period is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "tariff.not_found",
        "The tariff was not found.",
        ErrorType.NotFound);

    public static Error VersionNotFound { get; } = new(
        "tariff.version_not_found",
        "The tariff version was not found.",
        ErrorType.NotFound);

    public static Error NameAlreadyExists { get; } = new(
        "tariff.name_already_exists",
        "A tariff with the same name already exists.",
        ErrorType.Conflict);

    public static Error VersionAlreadyClosed { get; } = new(
        "tariff.version_already_closed",
        "The tariff version is already closed at an earlier date.",
        ErrorType.Conflict);

    public static Error OverlappingPeriod { get; } = new(
        "tariff.version_period_overlap",
        "The tariff already has a version overlapping the requested period.",
        ErrorType.Conflict);
}
