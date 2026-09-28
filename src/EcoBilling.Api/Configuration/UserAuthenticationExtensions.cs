using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Modules.Identity.Application;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Application.Tokens;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EcoBilling.Api.Configuration;

public static class UserAuthenticationExtensions
{
    public static IServiceCollection AddUserAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration
            .GetRequiredSection(UserAuthenticationOptions.SectionName)
            .Get<UserAuthenticationOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{UserAuthenticationOptions.SectionName}' is required.");
        var signingKeys = ValidateAndCreateKeys(settings);
        var activeSigningKey = signingKeys.Single(key =>
            string.Equals(key.KeyId, settings.ActiveSigningKeyId, StringComparison.Ordinal));

        services
            .AddAuthentication()
            .AddJwtBearer(
                UserAuthenticationOptions.Scheme,
                options => ConfigureBearer(options, settings, signingKeys));
        services.AddAuthorizationBuilder()
            .AddPolicy(
                UserAuthenticationOptions.AuthenticatedPolicy,
                policy => ConfigureRolePolicy(policy, role: null))
            .AddPolicy(
                UserAuthenticationOptions.DirectorPolicy,
                policy => ConfigureRolePolicy(policy, UserRole.Director))
            .AddPolicy(
                UserAuthenticationOptions.ControllerPolicy,
                policy => ConfigureRolePolicy(policy, UserRole.Controller))
            .AddPolicy(
                UserAuthenticationOptions.ResidentPolicy,
                policy => ConfigureRolePolicy(policy, UserRole.Resident));

        services.AddSingleton<IAccessTokenIssuer>(
            new AccessTokenIssuer(
                settings.Issuer,
                settings.Audience,
                settings.AccessTokenLifetime,
                activeSigningKey));
        services.AddSingleton(
            new AuthenticationPolicy(
                settings.MaximumFailedAttempts,
                settings.LockoutDuration));
        services.AddSingleton(new TokenPolicy(settings.RefreshTokenLifetime));
        services.AddSingleton(
            new PasswordPolicy(
                settings.MinimumPasswordLength,
                settings.MaximumPasswordLength));
        services.AddSingleton<ILoginNormalizer, LoginNormalizer>();
        services.AddScoped<AuthenticateHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<RevokeRefreshTokenHandler>();
        services.AddScoped<SetInitialPasswordHandler>();

        return services;
    }

    private static IReadOnlyList<SymmetricSecurityKey> ValidateAndCreateKeys(
        UserAuthenticationOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Issuer) ||
            string.IsNullOrWhiteSpace(settings.Audience) ||
            string.IsNullOrWhiteSpace(settings.ActiveSigningKeyId) ||
            settings.AccessTokenLifetime <= TimeSpan.Zero ||
            settings.RefreshTokenLifetime <= TimeSpan.Zero ||
            settings.ClockSkew < TimeSpan.Zero ||
            settings.MaximumFailedAttempts < 1 ||
            settings.LockoutDuration <= TimeSpan.Zero ||
            settings.MinimumPasswordLength < 1 ||
            settings.MaximumPasswordLength < settings.MinimumPasswordLength ||
            settings.SigningKeys.Count == 0 ||
            settings.SigningKeys.Select(key => key.KeyId).Distinct(StringComparer.Ordinal).Count() !=
            settings.SigningKeys.Count)
        {
            throw new InvalidOperationException("User authentication configuration is invalid.");
        }

        var keys = settings.SigningKeys.Select(CreateKey).ToArray();
        if (keys.Count(key =>
                string.Equals(key.KeyId, settings.ActiveSigningKeyId, StringComparison.Ordinal)) != 1)
        {
            throw new InvalidOperationException(
                "User authentication ActiveSigningKeyId must identify exactly one signing key.");
        }

        return keys;
    }

    private static SymmetricSecurityKey CreateKey(UserSigningKeyOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.KeyId) ||
            string.IsNullOrWhiteSpace(settings.SecretBase64))
        {
            throw new InvalidOperationException(
                "Every user authentication signing key requires KeyId and SecretBase64.");
        }

        byte[] secret;
        try
        {
            secret = Convert.FromBase64String(settings.SecretBase64);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"User authentication signing key '{settings.KeyId}' is not valid Base64.",
                exception);
        }

        if (secret.Length < 32)
        {
            throw new InvalidOperationException(
                $"User authentication signing key '{settings.KeyId}' must contain at least 32 random bytes.");
        }

        return new SymmetricSecurityKey(secret) { KeyId = settings.KeyId };
    }

    private static void ConfigureBearer(
        JwtBearerOptions options,
        UserAuthenticationOptions settings,
        IReadOnlyList<SymmetricSecurityKey> signingKeys)
    {
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            TryAllIssuerSigningKeys = false,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = settings.ClockSkew,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteProblemAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "auth.unauthorized",
                    "A valid user access token is required.");
            },
            OnForbidden = context => WriteProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "auth.forbidden",
                "The authenticated user cannot perform this operation.")
        };
    }

    private static void ConfigureRolePolicy(
        Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder policy,
        UserRole? role)
    {
        policy.AddAuthenticationSchemes(UserAuthenticationOptions.Scheme);
        policy.RequireAuthenticatedUser();
        if (role is not null)
        {
            policy.RequireRole(role.Value.ToString());
        }
    }

    private static Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string code,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status401Unauthorized
                ? "Unauthorized"
                : "Forbidden",
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}
