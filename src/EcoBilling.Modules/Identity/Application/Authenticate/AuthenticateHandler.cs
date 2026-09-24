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

    public AuthenticateHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        ILoginNormalizer loginNormalizer)
    {
        this.userAccountRepository = userAccountRepository
            ?? throw new ArgumentNullException(nameof(userAccountRepository));
        this.passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.loginNormalizer = loginNormalizer
            ?? throw new ArgumentNullException(nameof(loginNormalizer));
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

        var userAccount = await userAccountRepository.FindByLoginAsync(
            command.LoginType,
            normalizedLogin.Value,
            cancellationToken);

        if (userAccount is null ||
            !passwordHasher.Verify(command.Password, userAccount.PasswordHash))
        {
            return Result<AuthenticationResult>.Failure(IdentityErrors.InvalidCredentials);
        }

        var authenticatedUser = new AuthenticatedUser(userAccount.Id, userAccount.Role);
        return Result<AuthenticationResult>.Success(
            new AuthenticationResult(authenticatedUser));
    }
}
