using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Identity.Domain;

public static class IdentityErrors
{
    public static Error InvalidCredentials { get; } = new(
        "auth.invalid_credentials",
        "The login or password is invalid.",
        ErrorType.Unauthorized);

    public static Error InvalidLogin { get; } = new(
        "auth.invalid_login",
        "The login identifier is invalid.",
        ErrorType.Validation);

    public static Error InvalidRole { get; } = new(
        "auth.invalid_role",
        "The user role is invalid.",
        ErrorType.Validation);

    public static Error InvalidPasswordHash { get; } = new(
        "auth.invalid_password_hash",
        "The password hash is invalid.",
        ErrorType.Validation);

    public static Error PasswordSetupRequired { get; } = new(
        "auth.password_setup_required",
        "The initial credential must be replaced before authentication.",
        ErrorType.Forbidden);

    public static Error InvalidRefreshToken { get; } = new(
        "auth.invalid_refresh_token",
        "The refresh credential is invalid or no longer active.",
        ErrorType.Unauthorized);

    public static Error InvalidPassword { get; } = new(
        "auth.invalid_password",
        "The new password does not satisfy the current password policy.",
        ErrorType.Validation);

    public static Error PasswordSetupNotAvailable { get; } = new(
        "auth.password_setup_not_available",
        "Initial password setup is not available for this account.",
        ErrorType.Forbidden);
}
