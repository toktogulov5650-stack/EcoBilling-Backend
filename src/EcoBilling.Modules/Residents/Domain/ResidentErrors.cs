using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Residents.Domain;

public static class ResidentErrors
{
    public static Error InvalidFullName { get; } = new(
        "resident.invalid_full_name",
        "The resident full name is invalid.",
        ErrorType.Validation);

    public static Error IdentityMustHaveResidentRole { get; } = new(
        "resident.identity_role_mismatch",
        "The linked identity must have the Resident role.",
        ErrorType.Validation);

    public static Error NotFound { get; } = new(
        "resident.not_found",
        "The resident was not found.",
        ErrorType.NotFound);
}
