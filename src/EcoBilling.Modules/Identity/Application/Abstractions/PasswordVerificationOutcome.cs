namespace EcoBilling.Modules.Identity.Application.Abstractions;

public enum PasswordVerificationOutcome
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}
