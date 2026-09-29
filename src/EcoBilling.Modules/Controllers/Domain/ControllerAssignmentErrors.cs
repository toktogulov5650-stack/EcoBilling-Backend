using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Controllers.Domain;

public static class ControllerAssignmentErrors
{
    public static Error NotFound { get; } = new(
        "controller.assignment.not_found",
        "The controller assignment was not found.",
        ErrorType.NotFound);

    public static Error AlreadyExists { get; } = new(
        "controller.assignment.already_exists",
        "The controller is already assigned to this address.",
        ErrorType.Conflict);

    public static Error ControllerNotFound { get; } = new(
        "controller.assignment.controller_not_found",
        "The controller was not found.",
        ErrorType.NotFound);

    public static Error AddressNotFound { get; } = new(
        "controller.assignment.address_not_found",
        "The address was not found.",
        ErrorType.NotFound);
}
