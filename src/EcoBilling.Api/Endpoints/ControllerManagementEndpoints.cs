using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Controllers.Features.CreateController;

namespace EcoBilling.Api.Endpoints;

public static class ControllerManagementEndpoints
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapControllerManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/controllers", CreateAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("CreateController")
            .WithTags("Controllers")
            .Produces<CreateControllerResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateControllerRequest request,
        HttpContext httpContext,
        CreateControllerHandler handler,
        CancellationToken cancellationToken)
    {
        var actorId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new InvalidOperationException(
                "Authenticated director is missing the subject claim.");
        }

        var result = await handler.Handle(
            new CreateControllerCommand(
                httpContext.Request.Headers[IdempotencyKeyHeaderName].ToString(),
                request.FullName,
                request.Email,
                request.InitialCredential,
                actorId,
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
            new CreateControllerResponse(
                result.Value.ControllerId.Value,
                result.Value.OperationId.Value,
                "created"),
            statusCode: StatusCodes.Status201Created);
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
            "controller.creation.invalid_idempotency_key" =>
                IdempotencyKeyHeaderName,
            "controller.invalid_full_name" => "fullName",
            "auth.invalid_login" => "email",
            "controller.invalid_initial_credential" => "initialCredential",
            _ => "request"
        };

        return new Dictionary<string, string[]>
        {
            [field] = [error.Description]
        };
    }
}

public sealed record CreateControllerRequest(
    string? FullName,
    string? Email,
    string? InitialCredential)
{
    public override string ToString() =>
        $"{nameof(CreateControllerRequest)} {{ FullName = [REDACTED], Email = [REDACTED], InitialCredential = [REDACTED] }}";
}

public sealed record CreateControllerResponse(
    Guid ControllerId,
    Guid OperationId,
    string Status);
