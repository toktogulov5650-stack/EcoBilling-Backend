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

    public static Error InvalidPassword { get; } = new(
        "resident.invalid_password",
        "The resident password does not satisfy the current password policy.",
        ErrorType.Validation);

    public static Error InvalidCreationIdempotencyKey { get; } = new(
        "resident.creation.invalid_idempotency_key",
        "A valid resident creation idempotency key is required.",
        ErrorType.Validation);

    public static Error InvalidCreationRequestFingerprint { get; } = new(
        "resident.creation.invalid_request_fingerprint",
        "The resident creation request fingerprint is invalid.",
        ErrorType.Validation);

    public static Error CreationIdempotencyConflict { get; } = new(
        "resident.creation.idempotency_conflict",
        "The idempotency key was already used with a different resident request.",
        ErrorType.Conflict);

    public static Error AccountNumberAlreadyExists { get; } = new(
        "resident.account_number_already_exists",
        "A resident with this account number already exists.",
        ErrorType.Conflict);

    public static Error InvalidPasswordResetIdempotencyKey { get; } = new(
        "resident.password_reset.invalid_idempotency_key",
        "A valid resident password reset idempotency key is required.",
        ErrorType.Validation);

    public static Error InvalidPasswordResetRequestFingerprint { get; } = new(
        "resident.password_reset.invalid_request_fingerprint",
        "The resident password reset request fingerprint is invalid.",
        ErrorType.Validation);

    public static Error PasswordResetIdempotencyConflict { get; } = new(
        "resident.password_reset.idempotency_conflict",
        "The idempotency key was already used with a different password reset request.",
        ErrorType.Conflict);
}
