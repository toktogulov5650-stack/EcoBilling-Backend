using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Identity.Application.ProvisionDirector;
using EcoBilling.Modules.Identity.Application.ResetDirectorPassword;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Api.InternalEndpoints;

public static class DirectorProvisioningEndpoints
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapDirectorProvisioningEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/internal/v1/directors",
                ProvisionAsync)
            .RequireAuthorization(InternalServiceAuthenticationOptions.Policy)
            .WithName("ProvisionDirector")
            .WithTags("Internal")
            .Produces<ProvisionDirectorResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost(
                "/internal/v1/directors/reset-password",
                ResetPasswordAsync)
            .RequireAuthorization(InternalServiceAuthenticationOptions.Policy)
            .WithName("ResetDirectorPassword")
            .WithTags("Internal")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> ProvisionAsync(
        ProvisionDirectorRequest request,
        HttpContext httpContext,
        ProvisionDirectorHandler handler,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = httpContext.Request
            .Headers[IdempotencyKeyHeaderName]
            .ToString();
        var result = await handler.Handle(
            new ProvisionDirectorCommand(
                idempotencyKey,
                request.FullName,
                request.Email,
                request.Password,
                httpContext.User.FindFirst(JwtRegisteredClaimNames.Iss)?.Value
                    ?? throw new InvalidOperationException(
                        "Authenticated internal service is missing its issuer."),
                httpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(
                httpContext,
                result.Error,
                CreateValidationErrors(result.Error));
        }

        httpContext.Response.Headers["Idempotency-Replayed"] =
            result.Value.IsReplay ? "true" : "false";

        return Results.Json(
            new ProvisionDirectorResponse(
                result.Value.DirectorId.Value,
                result.Value.OperationId.Value,
                "created"),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetDirectorPasswordRequest request,
        HttpContext httpContext,
        ResetDirectorPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = httpContext.Request
            .Headers[IdempotencyKeyHeaderName]
            .ToString();
        var result = await handler.Handle(
            new ResetDirectorPasswordCommand(
                idempotencyKey,
                request.Email,
                request.NewPassword,
                httpContext.User.FindFirst(JwtRegisteredClaimNames.Iss)?.Value
                    ?? throw new InvalidOperationException(
                        "Authenticated internal service is missing its issuer."),
                httpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(
                httpContext,
                result.Error,
                CreatePasswordResetValidationErrors(result.Error));
        }

        httpContext.Response.Headers["Idempotency-Replayed"] =
            result.Value.IsReplay ? "true" : "false";
        return Results.NoContent();
    }

    private static IReadOnlyDictionary<string, string[]>? CreateValidationErrors(
        EcoBilling.SharedKernel.Errors.Error error)
    {
        if (error.Type is not EcoBilling.SharedKernel.Errors.ErrorType.Validation)
        {
            return null;
        }

        var field = error.Code switch
        {
            "operation.invalid_idempotency_key" => IdempotencyKeyHeaderName,
            "director.invalid_full_name" => "fullName",
            "auth.invalid_login" => "email",
            "director.invalid_password" => "password",
            _ => "request"
        };

        return new Dictionary<string, string[]>
        {
            [field] = [error.Description]
        };
    }

    private static IReadOnlyDictionary<string, string[]>? CreatePasswordResetValidationErrors(
        EcoBilling.SharedKernel.Errors.Error error)
    {
        if (error.Type is not EcoBilling.SharedKernel.Errors.ErrorType.Validation)
        {
            return null;
        }

        var field = error.Code switch
        {
            "operation.invalid_idempotency_key" => IdempotencyKeyHeaderName,
            "auth.invalid_login" => "email",
            "director.invalid_password" => "newPassword",
            _ => "request"
        };

        return new Dictionary<string, string[]>
        {
            [field] = [error.Description]
        };
    }
}

public sealed class ProvisionDirectorRequest
{
    public ProvisionDirectorRequest(
        string? fullName,
        string? email,
        string? password)
    {
        FullName = fullName;
        Email = email;
        Password = password;
    }

    public string? FullName { get; }

    public string? Email { get; }

    public string? Password { get; }

    public override string ToString() =>
        $"{nameof(ProvisionDirectorRequest)} {{ FullName = [REDACTED], Email = [REDACTED], Password = [REDACTED] }}";
}

public sealed record ProvisionDirectorResponse(
    Guid DirectorId,
    Guid OperationId,
    string Status);

public sealed class ResetDirectorPasswordRequest
{
    public ResetDirectorPasswordRequest(string? email, string? newPassword)
    {
        Email = email;
        NewPassword = newPassword;
    }

    public string? Email { get; }

    public string? NewPassword { get; }

    public override string ToString() =>
        $"{nameof(ResetDirectorPasswordRequest)} {{ Email = [REDACTED], NewPassword = [REDACTED] }}";
}
