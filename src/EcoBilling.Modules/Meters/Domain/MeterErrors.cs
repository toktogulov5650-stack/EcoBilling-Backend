using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Meters.Domain;

public static class MeterErrors
{
    public static Error InvalidSerialNumber { get; } = new(
        "meter.invalid_serial_number",
        "The meter serial number is invalid.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "meter.not_found",
        "The meter was not found.",
        ErrorType.NotFound);

    public static Error AccountNotFound { get; } = new(
        "meter.account_not_found",
        "The account was not found.",
        ErrorType.NotFound);

    public static Error SerialNumberAlreadyExists { get; } = new(
        "meter.serial_number_already_exists",
        "A meter with the same serial number already exists.",
        ErrorType.Conflict);

    public static Error AlreadyRetired { get; } = new(
        "meter.already_retired",
        "The meter is already retired.",
        ErrorType.Conflict);

    public static Error InvalidRetiredAt { get; } = new(
        "meter.invalid_retired_at",
        "The meter retirement time cannot precede installation.",
        ErrorType.Validation);

    public static Error ReplacementMeterMismatch { get; } = new(
        "meter.replacement_account_mismatch",
        "The replacement meter must belong to the same account.",
        ErrorType.Validation);

    public static Error AccessDenied { get; } = new(
        "meter.access_denied",
        "The controller is not assigned to the address served by this meter.",
        ErrorType.Forbidden);
}
