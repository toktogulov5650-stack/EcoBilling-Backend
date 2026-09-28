using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.PasswordSetup;

public sealed class SetInitialPasswordHandler
{
    private readonly IUserAccountRepository userAccountRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILoginNormalizer loginNormalizer;
    private readonly PasswordPolicy passwordPolicy;
    private readonly AuthenticationPolicy authenticationPolicy;
    private readonly TimeProvider timeProvider;

    public SetInitialPasswordHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        ILoginNormalizer loginNormalizer,
        PasswordPolicy passwordPolicy,
        AuthenticationPolicy authenticationPolicy,
        TimeProvider timeProvider)
    {
        this.userAccountRepository = userAccountRepository;
        this.passwordHasher = passwordHasher;
        this.loginNormalizer = loginNormalizer;
        this.passwordPolicy = passwordPolicy;
        this.authenticationPolicy = authenticationPolicy;
        this.timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        SetInitialPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!passwordPolicy.IsValid(command.NewPassword) ||
            string.Equals(
                command.InitialCredential,
                command.NewPassword,
                StringComparison.Ordinal))
        {
            return Result.Failure(IdentityErrors.InvalidPassword);
        }

        var normalizedLogin = loginNormalizer.Normalize(command.LoginType, command.Login);
        if (normalizedLogin.IsFailure || string.IsNullOrWhiteSpace(command.InitialCredential))
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var account = await userAccountRepository.GetByLoginAsync(
            normalizedLogin.Value,
            cancellationToken);
        if (account is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();
        if (account.IsLockedOut(now))
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var verification = passwordHasher.Verify(
            command.InitialCredential,
            account.PasswordHash);
        if (verification is PasswordVerificationOutcome.Failed)
        {
            account.RecordFailedLogin(
                now,
                authenticationPolicy.MaximumFailedAttempts,
                authenticationPolicy.LockoutDuration);
            await userAccountRepository.SaveAsync(account, cancellationToken);
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        if (!account.RequiresPasswordChange || account.Role is UserRole.Resident)
        {
            return Result.Failure(IdentityErrors.PasswordSetupNotAvailable);
        }

        account.CompletePasswordSetup(passwordHasher.Hash(command.NewPassword!));
        await userAccountRepository.SaveAsync(account, cancellationToken);
        return Result.Success();
    }
}
