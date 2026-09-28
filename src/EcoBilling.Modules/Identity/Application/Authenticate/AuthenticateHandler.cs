using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.Authenticate;

public sealed class AuthenticateHandler
{
    private readonly IUserAccountRepository userAccountRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILoginNormalizer loginNormalizer;
    private readonly AuthenticationPolicy policy;
    private readonly TimeProvider timeProvider;

    public AuthenticateHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        ILoginNormalizer loginNormalizer,
        AuthenticationPolicy policy,
        TimeProvider timeProvider)
    {
        this.userAccountRepository = userAccountRepository
            ?? throw new ArgumentNullException(nameof(userAccountRepository));
        this.passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.loginNormalizer = loginNormalizer
            ?? throw new ArgumentNullException(nameof(loginNormalizer));
        this.policy = policy
            ?? throw new ArgumentNullException(nameof(policy));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<AuthenticationResult>> Handle(
        AuthenticateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<AuthenticationResult>.Failure(IdentityErrors.InvalidCredentials);
        }

        var normalizedLogin = loginNormalizer.Normalize(command.LoginType, command.Login);
        if (normalizedLogin.IsFailure)
        {
            return Result<AuthenticationResult>.Failure(normalizedLogin.Error);
        }

        var userAccount = await userAccountRepository.GetByLoginAsync(
            normalizedLogin.Value,
            cancellationToken);

        if (userAccount is null)
        {
            return Result<AuthenticationResult>.Failure(IdentityErrors.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();
        if (userAccount.IsLockedOut(now))
        {
            return Result<AuthenticationResult>.Failure(IdentityErrors.InvalidCredentials);
        }

        var verificationOutcome = passwordHasher.Verify(
            command.Password,
            userAccount.PasswordHash);

        if (verificationOutcome is PasswordVerificationOutcome.Failed)
        {
            userAccount.RecordFailedLogin(
                now,
                policy.MaximumFailedAttempts,
                policy.LockoutDuration);
            await userAccountRepository.SaveAsync(
                userAccount,
                cancellationToken);
            return Result<AuthenticationResult>.Failure(IdentityErrors.InvalidCredentials);
        }

        if (userAccount.RequiresPasswordChange)
        {
            return Result<AuthenticationResult>.Failure(IdentityErrors.PasswordSetupRequired);
        }

        if (verificationOutcome is PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            userAccount.ReplacePasswordHash(passwordHasher.Hash(command.Password));
        }

        userAccount.RecordSuccessfulLogin();
        await userAccountRepository.SaveAsync(
            userAccount,
            cancellationToken);

        var authenticatedUser = new AuthenticatedUser(userAccount.Id, userAccount.Role);
        return Result<AuthenticationResult>.Success(
            new AuthenticationResult(authenticatedUser));
    }
}
