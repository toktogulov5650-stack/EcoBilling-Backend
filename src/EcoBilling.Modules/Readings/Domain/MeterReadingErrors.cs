using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Readings.Domain;

public static class MeterReadingErrors
{
    public static Error InvalidValue { get; } = new(
        "reading.invalid_value",
        "The meter reading value is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "reading.not_found",
        "The meter reading was not found.",
        ErrorType.NotFound);

    public static Error MeterNotFound { get; } = new(
        "reading.meter_not_found",
        "The meter was not found.",
        ErrorType.NotFound);

    public static Error MeterInactive { get; } = new(
        "reading.meter_inactive",
        "A reading cannot be added to an inactive meter.",
        ErrorType.Conflict);

    public static Error DecreasedValue { get; } = new(
        "reading.value_decreased",
        "A normal reading cannot be lower than the previous accepted reading.",
        ErrorType.Validation);

    public static Error BackdatedReadingRequiresDirector { get; } = new(
        "reading.backdated_requires_director",
        "A backdated reading requires Director authority and a reason.",
        ErrorType.Forbidden);

    public static Error BackdatedReasonRequired { get; } = new(
        "reading.backdated_reason_required",
        "A backdated reading requires a reason.",
        ErrorType.Validation);

    public static Error InvalidSource { get; } = new(
        "reading.invalid_source",
        "The reading source is invalid.",
        ErrorType.Validation);

    public static Error InvalidCorrection { get; } = new(
        "reading.invalid_correction",
        "A correction must reference the reading it supersedes and include a reason.",
        ErrorType.Validation);

    public static Error InvalidCorrectionReason { get; } = new(
        "reading.invalid_correction_reason",
        "The correction reason is invalid.",
        ErrorType.Validation);

    public static Error AlreadyCorrected { get; } = new(
        "reading.already_corrected",
        "The selected reading already has a correction.",
        ErrorType.Conflict);

    public static Error AccessDenied { get; } = new(
        "reading.access_denied",
        "The controller is not assigned to this meter's address.",
        ErrorType.Forbidden);
}
