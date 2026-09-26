using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Domain;

public sealed class UserAccount
{
    private UserAccount()
    {
        Id = null!;
        LoginIdentity = null!;
        PasswordHash = string.Empty;
    }

    private UserAccount(
        UserId id,
        LoginIdentity loginIdentity,
        string passwordHash,
        UserRole role,
        bool requiresPasswordChange,
        DateTimeOffset createdAt)
    {
        Id = id;
        LoginIdentity = loginIdentity;
        PasswordHash = passwordHash;
        Role = role;
        RequiresPasswordChange = requiresPasswordChange;
        CreatedAt = createdAt;
    }

    public UserId Id { get; private set; }

    public LoginIdentity LoginIdentity { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool RequiresPasswordChange { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<UserAccount> Create(
        UserId id,
        LoginIdentity? loginIdentity,
        string? passwordHash,
        UserRole role,
        DateTimeOffset createdAt,
        bool requiresPasswordChange = false)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!Enum.IsDefined(role))
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidRole);
        }

        if (loginIdentity is null)
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidLogin);
        }

        if (!loginIdentity.IsCompatibleWith(role))
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidRole);
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidPasswordHash);
        }

        return Result<UserAccount>.Success(
            new UserAccount(
                id,
                loginIdentity,
                passwordHash,
                role,
                requiresPasswordChange,
                createdAt.ToUniversalTime()));
    }
}
