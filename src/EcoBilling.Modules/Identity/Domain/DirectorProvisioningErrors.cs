using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.Modules.Identity.Domain;

public static class DirectorProvisioningErrors
{
    public static Error InvalidFullName { get; } = new(
        "director.invalid_full_name",
        "The director full name is invalid.",
        ErrorType.Validation);

    public static Error InvalidInitialCredential { get; } = new(
        "director.invalid_initial_credential",
        "The initial director credential is invalid.",
        ErrorType.Validation);

    public static Error IdentityMustHaveDirectorRole { get; } = new(
        "director.identity_role_mismatch",
        "The linked identity must have the Director role.",
        ErrorType.Validation);

    public static Error InvalidIdempotencyKey { get; } = new(
        "operation.invalid_idempotency_key",
        "The idempotency key is invalid.",
        ErrorType.Validation);

    public static Error InvalidRequestFingerprint { get; } = new(
        "operation.invalid_request_fingerprint",
        "The operation request fingerprint is invalid.",
        ErrorType.Validation);

    public static Error IdempotencyConflict { get; } = new(
        "operation.idempotency_conflict",
        "The idempotency key has already been used for another request.",
        ErrorType.Conflict);

    public static Error DirectorAlreadyExists { get; } = new(
        "director.already_exists",
        "A director or the requested director email already exists.",
        ErrorType.Conflict);
}
