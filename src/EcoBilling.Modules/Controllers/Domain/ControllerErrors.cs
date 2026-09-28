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

    public static Error InvalidInitialCredential { get; } = new(
        "controller.invalid_initial_credential",
        "The controller initial credential is invalid.",
        ErrorType.Validation);

    public static Error InvalidCreationIdempotencyKey { get; } = new(
        "controller.creation.invalid_idempotency_key",
        "A valid controller creation idempotency key is required.",
        ErrorType.Validation);

    public static Error InvalidCreationRequestFingerprint { get; } = new(
        "controller.creation.invalid_request_fingerprint",
        "The controller creation request fingerprint is invalid.",
        ErrorType.Validation);

    public static Error CreationIdempotencyConflict { get; } = new(
        "controller.creation.idempotency_conflict",
        "The idempotency key was already used with a different controller request.",
        ErrorType.Conflict);

    public static Error EmailAlreadyExists { get; } = new(
        "controller.email_already_exists",
        "A user with this controller email already exists.",
        ErrorType.Conflict);
}
