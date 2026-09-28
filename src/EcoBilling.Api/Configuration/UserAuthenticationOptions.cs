namespace EcoBilling.Api.Configuration;

public sealed class UserAuthenticationOptions
{
    public const string SectionName = "UserAuthentication";
    public const string Scheme = "UserBearer";
    public const string AuthenticatedPolicy = "User.Authenticated";
    public const string DirectorPolicy = "User.Director";
    public const string ControllerPolicy = "User.Controller";
    public const string ResidentPolicy = "User.Resident";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string ActiveSigningKeyId { get; init; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);

    public int MaximumFailedAttempts { get; init; } = 5;

    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);

    public int MinimumPasswordLength { get; init; } = 12;

    public int MaximumPasswordLength { get; init; } = 256;

    public IReadOnlyList<UserSigningKeyOptions> SigningKeys { get; init; } = [];
}

public sealed class UserSigningKeyOptions
{
    public string KeyId { get; init; } = string.Empty;

    public string SecretBase64 { get; init; } = string.Empty;
}
