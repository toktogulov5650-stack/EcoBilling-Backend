using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Controllers.Domain;

public static class ControllerErrors
{
    public static Error InvalidFullName { get; } = new(
        "controller.invalid_full_name",
        "The controller full name is invalid.",
        ErrorType.Validation);

    public static Error IdentityMustHaveControllerRole { get; } = new(
        "controller.identity_role_mismatch",
        "The linked identity must have the Controller role.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "controller.not_found",
        "The controller was not found.",
        ErrorType.NotFound);
}
