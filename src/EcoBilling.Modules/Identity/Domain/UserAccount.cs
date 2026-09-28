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
        DateTimeOffset createdAt,
        int failedLoginAttempts,
        DateTimeOffset? lockoutEnd)
    {
        Id = id;
        LoginIdentity = loginIdentity;
        PasswordHash = passwordHash;
        Role = role;
        RequiresPasswordChange = requiresPasswordChange;
        CreatedAt = createdAt;
        FailedLoginAttempts = failedLoginAttempts;
        LockoutEnd = lockoutEnd;
    }

    public UserId Id { get; private set; }

    public LoginIdentity LoginIdentity { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool RequiresPasswordChange { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

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
                createdAt.ToUniversalTime(),
                failedLoginAttempts: 0,
                lockoutEnd: null));
    }

    public bool IsLockedOut(DateTimeOffset now) =>
        LockoutEnd is not null && LockoutEnd > now.ToUniversalTime();

    public void RecordFailedLogin(
        DateTimeOffset now,
        int maximumFailedAttempts,
        TimeSpan lockoutDuration)
    {
        if (maximumFailedAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFailedAttempts));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        var utcNow = now.ToUniversalTime();
        if (LockoutEnd is not null && LockoutEnd <= utcNow)
        {
            FailedLoginAttempts = 0;
            LockoutEnd = null;
        }

        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maximumFailedAttempts)
        {
            LockoutEnd = utcNow.Add(lockoutDuration);
        }
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void ReplacePasswordHash(string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    public void CompletePasswordSetup(string? passwordHash)
    {
        if (!RequiresPasswordChange || Role is UserRole.Resident)
        {
            throw new InvalidOperationException(
                "Password setup is available only for staff accounts that require a password change.");
        }

        ReplacePasswordHash(passwordHash);
        RequiresPasswordChange = false;
        RecordSuccessfulLogin();
    }
}
