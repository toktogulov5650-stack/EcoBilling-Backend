using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.AssignAddress;
using EcoBilling.Modules.Controllers.Features.CreateController;
using EcoBilling.Modules.Controllers.Features.GetMyAssignments;
using EcoBilling.Modules.Controllers.Features.RemoveAssignment;
using EcoBilling.Modules.Identity.Domain;

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

        endpoints.MapPost(
                "/api/v1/controllers/{controllerId:guid}/assignments",
                AssignAddressAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("AssignControllerAddress")
            .WithTags("Controllers")
            .Produces<ControllerAssignmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapDelete(
                "/api/v1/controllers/{controllerId:guid}/assignments/{assignmentId:guid}",
                RemoveAssignmentAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("RemoveControllerAssignment")
            .WithTags("Controllers")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/controllers/me/assignments",
                GetMyAssignmentsAsync)
            .RequireAuthorization(UserAuthenticationOptions.ControllerPolicy)
            .WithName("GetMyControllerAssignments")
            .WithTags("Controllers")
            .Produces<IReadOnlyList<ControllerAssignmentResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> AssignAddressAsync(
        Guid controllerId,
        AssignAddressRequest request,
        HttpContext httpContext,
        AssignAddressHandler handler,
        CancellationToken cancellationToken)
    {
        if (controllerId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                ControllerAssignmentErrors.ControllerNotFound);
        }

        if (request.AddressId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                ControllerAssignmentErrors.AddressNotFound);
        }

        var actorId = GetAuthenticatedSubject(
            httpContext,
            "Authenticated director is missing the subject claim.");

        var result = await handler.Handle(
            new AssignAddressCommand(
                new ControllerId(controllerId),
                new AddressId(request.AddressId),
                actorId,
                httpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(httpContext, result.Error);
        }

        httpContext.Response.Headers["Idempotency-Replayed"] =
            result.Value.IsReplay ? "true" : "false";

        return Results.Json(
            new ControllerAssignmentResponse(
                result.Value.AssignmentId.Value,
                result.Value.ControllerId.Value,
                result.Value.AddressId.Value,
                result.Value.CreatedAt),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> RemoveAssignmentAsync(
        Guid controllerId,
        Guid assignmentId,
        HttpContext httpContext,
        RemoveAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        if (controllerId == Guid.Empty || assignmentId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                ControllerAssignmentErrors.NotFound);
        }

        var actorId = GetAuthenticatedSubject(
            httpContext,
            "Authenticated director is missing the subject claim.");

        var result = await handler.Handle(
            new RemoveAssignmentCommand(
                new ControllerId(controllerId),
                new ControllerAssignmentId(assignmentId),
                actorId,
                httpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(httpContext, result.Error);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetMyAssignmentsAsync(
        HttpContext httpContext,
        GetMyAssignmentsHandler handler,
        CancellationToken cancellationToken)
    {
        var subject = GetAuthenticatedSubject(
            httpContext,
            "Authenticated controller is missing the subject claim.");

        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Authenticated controller has an invalid subject claim.");
        }

        var result = await handler.Handle(
            new GetMyAssignmentsQuery(new UserId(userId)),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(httpContext, result.Error);
        }

        return Results.Ok(
            result.Value.Select(
                assignment => new ControllerAssignmentResponse(
                    assignment.AssignmentId,
                    assignment.ControllerId,
                    assignment.AddressId,
                    assignment.CreatedAt,
                    new ControllerAssignmentAddressResponse(
                        assignment.Locality,
                        assignment.Street,
                        assignment.House,
                        assignment.Building,
                        assignment.Apartment))));
    }

    private static async Task<IResult> CreateAsync(
        CreateControllerRequest request,
        HttpContext httpContext,
        CreateControllerHandler handler,
        CancellationToken cancellationToken)
    {
        var actorId = GetAuthenticatedSubject(
            httpContext,
            "Authenticated director is missing the subject claim.");

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

    private static string GetAuthenticatedSubject(
        HttpContext httpContext,
        string errorMessage)
    {
        var actorId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return actorId;
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

public sealed record AssignAddressRequest(Guid AddressId);

public sealed record ControllerAssignmentResponse(
    Guid AssignmentId,
    Guid ControllerId,
    Guid AddressId,
    DateTimeOffset CreatedAt,
    ControllerAssignmentAddressResponse? Address = null);

public sealed record ControllerAssignmentAddressResponse(
    string Locality,
    string Street,
    string House,
    string? Building,
    string? Apartment);
