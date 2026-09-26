using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Identity.Application.ProvisionDirector;
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
                HandleAsync)
            .RequireAuthorization(InternalServiceAuthenticationOptions.Policy)
            .WithName("ProvisionDirector")
            .WithTags("Internal")
            .Produces<ProvisionDirectorResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
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
                request.InitialCredential),
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
            "director.invalid_initial_credential" => "initialCredential",
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
        string? initialCredential)
    {
        FullName = fullName;
        Email = email;
        InitialCredential = initialCredential;
    }

    public string? FullName { get; }

    public string? Email { get; }

    public string? InitialCredential { get; }

    public override string ToString() =>
        $"{nameof(ProvisionDirectorRequest)} {{ FullName = [REDACTED], Email = [REDACTED], InitialCredential = [REDACTED] }}";
}

public sealed record ProvisionDirectorResponse(
    Guid DirectorId,
    Guid OperationId,
    string Status);
