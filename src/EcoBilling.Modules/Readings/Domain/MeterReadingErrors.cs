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
}
