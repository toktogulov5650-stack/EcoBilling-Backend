using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Application.Tokens;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Api.Endpoints;

public static class UserAuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapUserAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .AllowAnonymous()
            .RequireRateLimiting(ApiRateLimitingOptions.AuthenticationPolicy);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .Produces<TokenPairResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .Produces<TokenPairResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/setup-password", SetupPasswordAsync)
            .WithName("SetupInitialPassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/revoke", RevokeAsync)
            .WithName("RevokeRefreshToken")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        LoginHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseLoginType(request.LoginType, out var loginType))
        {
            return ApiProblemDetails.Create(
                httpContext,
                IdentityErrors.InvalidLogin,
                new Dictionary<string, string[]>
                {
                    ["loginType"] = ["Use 'email' or 'accountNumber'."]
                });
        }

        var result = await handler.Handle(
            new AuthenticateCommand(loginType, request.Login, request.Password),
            cancellationToken);
        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        RefreshHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(request.RefreshToken, cancellationToken);
        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> RevokeAsync(
        RefreshTokenRequest request,
        RevokeRefreshTokenHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(request.RefreshToken, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SetupPasswordAsync(
        SetInitialPasswordRequest request,
        SetInitialPasswordHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseLoginType(request.LoginType, out var loginType))
        {
            return ApiProblemDetails.Create(
                httpContext,
                IdentityErrors.InvalidLogin,
                new Dictionary<string, string[]>
                {
                    ["loginType"] = ["Use 'email' or 'accountNumber'."]
                });
        }

        var result = await handler.Handle(
            new SetInitialPasswordCommand(
                loginType,
                request.Login,
                request.InitialCredential,
                request.NewPassword),
            cancellationToken);
        return result.IsFailure
            ? ApiProblemDetails.Create(
                httpContext,
                result.Error,
                result.Error == IdentityErrors.InvalidPassword
                    ? new Dictionary<string, string[]>
                    {
                        ["newPassword"] = [result.Error.Description]
                    }
                    : null)
            : Results.NoContent();
    }

    private static TokenPairResponse ToResponse(TokenPairResult result) =>
        new(
            "Bearer",
            result.AccessToken,
            result.AccessTokenExpiresAt,
            result.RefreshToken,
            result.RefreshTokenExpiresAt,
            result.User.Role.ToString());

    private static bool TryParseLoginType(string? value, out LoginType loginType)
    {
        if (string.Equals(value, "email", StringComparison.OrdinalIgnoreCase))
        {
            loginType = LoginType.Email;
            return true;
        }

        if (string.Equals(value, "accountNumber", StringComparison.OrdinalIgnoreCase))
        {
            loginType = LoginType.AccountNumber;
            return true;
        }

        loginType = default;
        return false;
    }
}

public sealed record LoginRequest(string? LoginType, string? Login, string? Password)
{
    public override string ToString() =>
        $"{nameof(LoginRequest)} {{ LoginType = {LoginType}, Login = [REDACTED], Password = [REDACTED] }}";
}

public sealed record RefreshTokenRequest(string? RefreshToken)
{
    public override string ToString() =>
        $"{nameof(RefreshTokenRequest)} {{ RefreshToken = [REDACTED] }}";
}

public sealed record SetInitialPasswordRequest(
    string? LoginType,
    string? Login,
    string? InitialCredential,
    string? NewPassword)
{
    public override string ToString() =>
        $"{nameof(SetInitialPasswordRequest)} {{ LoginType = {LoginType}, Login = [REDACTED], InitialCredential = [REDACTED], NewPassword = [REDACTED] }}";
}

public sealed record TokenPairResponse(
    string TokenType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string Role);
