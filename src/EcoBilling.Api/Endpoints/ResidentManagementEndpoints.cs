using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.CreateResident;
using EcoBilling.Modules.Residents.Features.Directory;
using EcoBilling.Modules.Residents.Features.ResetPassword;

namespace EcoBilling.Api.Endpoints;

public static class ResidentManagementEndpoints
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapResidentManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/residents", ListResidentsAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("ListResidents")
            .WithTags("Residents");

        endpoints.MapGet("/api/v1/residents/{residentId:guid}", GetResidentByIdAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("GetResidentById")
            .WithTags("Residents")
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/v1/residents", CreateAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("CreateResident")
            .WithTags("Residents")
            .Produces<CreateResidentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPut(
                "/api/v1/residents/{residentId:guid}/password",
                ResetPasswordAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithName("ResetResidentPassword")
            .WithTags("Residents")
            .Produces<ResetResidentPasswordResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> ResetPasswordAsync(
        Guid residentId,
        ResetResidentPasswordRequest request,
        HttpContext httpContext,
        ResetResidentPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        if (residentId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, ResidentErrors.NotFound);
        }

        var actorId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new InvalidOperationException(
                "Authenticated director is missing the subject claim.");
        }

        var result = await handler.Handle(
            new ResetResidentPasswordCommand(
                new ResidentId(residentId),
                httpContext.Request.Headers[IdempotencyKeyHeaderName].ToString(),
                request.NewPassword,
                actorId,
                httpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(
                httpContext,
                result.Error,
                CreateResetPasswordValidationErrors(result.Error));
        }

        httpContext.Response.Headers["Idempotency-Replayed"] =
            result.Value.IsReplay ? "true" : "false";

        return Results.Ok(
            new ResetResidentPasswordResponse(
                result.Value.OperationId.Value,
                "reset"));
    }

    private static async Task<IResult> ListResidentsAsync(
        ListResidentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetResidentByIdAsync(
        Guid residentId,
        HttpContext httpContext,
        GetResidentByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (residentId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                EcoBilling.Modules.Residents.Domain.ResidentErrors.NotFound);
        }

        var result = await handler.Handle(
            new EcoBilling.Modules.Residents.Domain.ResidentId(residentId),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> CreateAsync(
        CreateResidentRequest request,
        HttpContext httpContext,
        CreateResidentHandler handler,
        CancellationToken cancellationToken)
    {
        var actorId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new InvalidOperationException(
                "Authenticated director is missing the subject claim.");
        }

        var result = await handler.Handle(
            new CreateResidentCommand(
                httpContext.Request.Headers[IdempotencyKeyHeaderName].ToString(),
                request.FullName,
                request.AccountNumber,
                request.Password,
                request.Address?.Locality,
                request.Address?.Street,
                request.Address?.House,
                request.Address?.Building,
                request.Address?.Apartment,
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
            new CreateResidentResponse(
                result.Value.ResidentId.Value,
                result.Value.AccountId.Value,
                result.Value.AddressId.Value,
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
            "resident.creation.invalid_idempotency_key" =>
                IdempotencyKeyHeaderName,
            "resident.password_reset.invalid_idempotency_key" =>
                IdempotencyKeyHeaderName,
            "resident.invalid_full_name" => "fullName",
            "auth.invalid_login" or "account.invalid_account_number" =>
                "accountNumber",
            "resident.invalid_password" => "password",
            "address.invalid_locality" => "address.locality",
            "address.invalid_street" => "address.street",
            "address.invalid_house" => "address.house",
            _ => "request"
        };

        return new Dictionary<string, string[]>
        {
            [field] = [error.Description]
        };
    }

    private static IReadOnlyDictionary<string, string[]>?
        CreateResetPasswordValidationErrors(
            EcoBilling.SharedKernel.Errors.Error error)
    {
        if (error.Type is not EcoBilling.SharedKernel.Errors.ErrorType.Validation)
        {
            return null;
        }

        var field = error.Code switch
        {
            "resident.password_reset.invalid_idempotency_key" =>
                IdempotencyKeyHeaderName,
            "resident.invalid_password" => "newPassword",
            _ => "request"
        };

        return new Dictionary<string, string[]>
        {
            [field] = [error.Description]
        };
    }
}

public sealed record CreateResidentRequest(
    string? FullName,
    string? AccountNumber,
    string? Password,
    CreateResidentAddressRequest? Address)
{
    public override string ToString() =>
        $"{nameof(CreateResidentRequest)} {{ FullName = [REDACTED], AccountNumber = [REDACTED], Password = [REDACTED], Address = [REDACTED] }}";
}

public sealed record CreateResidentAddressRequest(
    string? Locality,
    string? Street,
    string? House,
    string? Building,
    string? Apartment);

public sealed record CreateResidentResponse(
    Guid ResidentId,
    Guid AccountId,
    Guid AddressId,
    Guid OperationId,
    string Status);

public sealed record ResetResidentPasswordRequest(string? NewPassword)
{
    public override string ToString() =>
        $"{nameof(ResetResidentPasswordRequest)} {{ NewPassword = [REDACTED] }}";
}

public sealed record ResetResidentPasswordResponse(Guid OperationId, string Status);
