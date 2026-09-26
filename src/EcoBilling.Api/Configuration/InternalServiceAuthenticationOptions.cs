namespace EcoBilling.Api.Configuration;

public sealed class InternalServiceAuthenticationOptions
{
    public const string SectionName = "InternalServiceAuthentication";
    public const string Scheme = "InternalService";
    public const string Policy = "InternalService.Directors.Provision";
    public const string RequiredScope = "ecobilling.directors.provision";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public TimeSpan MaximumTokenLifetime { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);

    public IReadOnlyList<InternalServiceSigningKeyOptions> SigningKeys { get; init; } = [];
}

public sealed class InternalServiceSigningKeyOptions
{
    public string KeyId { get; init; } = string.Empty;

    public string PublicKeyPem { get; init; } = string.Empty;
}
