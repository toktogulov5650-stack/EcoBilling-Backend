using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using EcoBilling.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EcoBilling.Api.Configuration;

public static class InternalServiceAuthenticationExtensions
{
    public static IServiceCollection AddInternalServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration
            .GetRequiredSection(InternalServiceAuthenticationOptions.SectionName)
            .Get<InternalServiceAuthenticationOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{InternalServiceAuthenticationOptions.SectionName}' is required.");
        Validate(settings);

        var signingKeys = settings.SigningKeys
            .Select(CreateSigningKey)
            .ToArray();

        services
            .AddAuthentication(InternalServiceAuthenticationOptions.Scheme)
            .AddJwtBearer(
                InternalServiceAuthenticationOptions.Scheme,
                options =>
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
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ClockSkew = settings.ClockSkew
                    };
                    options.Events = CreateEvents(settings);
                });

        services.AddAuthorizationBuilder()
            .AddPolicy(
                InternalServiceAuthenticationOptions.Policy,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        InternalServiceAuthenticationOptions.Scheme);
                    policy.RequireAuthenticatedUser();
                });

        return services;
    }

    private static JwtBearerEvents CreateEvents(
        InternalServiceAuthenticationOptions settings) =>
        new()
        {
            OnTokenValidated = context => ValidateTokenAsync(context, settings),
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteAuthenticationProblemAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "service.unauthorized",
                    "The service credential is invalid.");
            },
            OnForbidden = context => WriteAuthenticationProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "service.forbidden",
                "The service credential is not allowed to perform this operation.")
        };

    private static async Task ValidateTokenAsync(
        TokenValidatedContext context,
        InternalServiceAuthenticationOptions settings)
    {
        var principal = context.Principal;
        var issuer = principal?.FindFirst(JwtRegisteredClaimNames.Iss)?.Value;
        var tokenId = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var issuedAtValue = principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
        var expiresAtValue = principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        var scopes = principal?.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(tokenId) ||
            !long.TryParse(
                issuedAtValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var issuedAtSeconds) ||
            !long.TryParse(
                expiresAtValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var expiresAtSeconds) ||
            scopes?.Contains(
                InternalServiceAuthenticationOptions.RequiredScope,
                StringComparer.Ordinal) is not true)
        {
            context.Fail("The service token is missing required claims.");
            return;
        }

        DateTimeOffset issuedAt;
        DateTimeOffset expiresAt;
        try
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds);
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            context.Fail("The service token has invalid timestamps.");
            return;
        }

        var timeProvider = context.HttpContext.RequestServices
            .GetRequiredService<TimeProvider>();
        var utcNow = timeProvider.GetUtcNow();
        if (expiresAt <= issuedAt ||
            expiresAt - issuedAt > settings.MaximumTokenLifetime ||
            issuedAt > utcNow + settings.ClockSkew)
        {
            context.Fail("The service token lifetime is invalid.");
            return;
        }

        var replayStore = context.HttpContext.RequestServices
            .GetRequiredService<IInternalServiceTokenReplayStore>();
        var consumed = await replayStore.TryConsumeAsync(
            issuer,
            tokenId,
            expiresAt + settings.ClockSkew,
            utcNow,
            context.HttpContext.RequestAborted);
        if (!consumed)
        {
            context.Fail("The service token has already been used.");
        }
    }

    private static RsaSecurityKey CreateSigningKey(
        InternalServiceSigningKeyOptions settings)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(settings.PublicKeyPem);

            try
            {
                _ = rsa.ExportParameters(includePrivateParameters: true);
                throw new InvalidOperationException(
                    $"Internal service signing key '{settings.KeyId}' must contain only a public RSA key.");
            }
            catch (CryptographicException)
            {
                // A public-only RSA key cannot export private parameters.
            }

            return new RsaSecurityKey(rsa)
            {
                KeyId = settings.KeyId
            };
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static void Validate(InternalServiceAuthenticationOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Issuer) ||
            string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new InvalidOperationException(
                "Internal service authentication Issuer and Audience are required.");
        }

        if (settings.MaximumTokenLifetime <= TimeSpan.Zero ||
            settings.ClockSkew < TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Internal service token lifetime must be positive and clock skew cannot be negative.");
        }

        if (settings.SigningKeys.Count == 0 ||
            settings.SigningKeys.Any(key =>
                string.IsNullOrWhiteSpace(key.KeyId) ||
                string.IsNullOrWhiteSpace(key.PublicKeyPem)) ||
            settings.SigningKeys
                .Select(key => key.KeyId)
                .Distinct(StringComparer.Ordinal)
                .Count() != settings.SigningKeys.Count)
        {
            throw new InvalidOperationException(
                "At least one uniquely identified internal service RSA public key is required.");
        }
    }

    private static Task WriteAuthenticationProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string code,
        string detail)
    {
        httpContext.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status401Unauthorized
                ? "Unauthorized"
                : "Forbidden",
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: httpContext.RequestAborted);
    }
}
