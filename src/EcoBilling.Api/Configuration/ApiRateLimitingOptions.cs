namespace EcoBilling.Api.Configuration;

public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public const string AuthenticationPolicy = "Authentication";

    public int AuthenticationPermitLimit { get; init; } = 20;

    public TimeSpan AuthenticationWindow { get; init; } = TimeSpan.FromMinutes(1);

    public int AuthenticationQueueLimit { get; init; } = 0;
}
