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
}
