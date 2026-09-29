using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.AssignToAccount;
using EcoBilling.Modules.Tariffs.Features.CloseAssignment;
using EcoBilling.Modules.Tariffs.Features.CloseVersion;
using EcoBilling.Modules.Tariffs.Features.Create;
using EcoBilling.Modules.Tariffs.Features.CreateVersion;
using EcoBilling.Modules.Tariffs.Features.GetById;
using EcoBilling.Modules.Tariffs.Features.GetForAccount;
using EcoBilling.Modules.Tariffs.Features.GetVersionById;
using EcoBilling.Modules.Tariffs.Features.List;
using EcoBilling.Modules.Tariffs.Features.ListAssignments;
using EcoBilling.Modules.Tariffs.Features.ListVersions;

namespace EcoBilling.Api.Endpoints;

public static class TariffManagementEndpoints
{
    public static IEndpointRouteBuilder MapTariffManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tariffs")
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Tariffs");

        group.MapPost("", CreateAsync).WithName("CreateTariff");
        group.MapGet("", ListAsync).WithName("ListTariffs");
        group.MapGet("/{tariffId:guid}", GetByIdAsync)
            .WithName("GetTariffById");
        group.MapPost("/{tariffId:guid}/versions", CreateVersionAsync)
            .WithName("CreateTariffVersion");
        group.MapGet("/{tariffId:guid}/versions", ListVersionsAsync)
            .WithName("ListTariffVersions");
        group.MapGet("/{tariffId:guid}/versions/{versionId:guid}", GetVersionByIdAsync)
            .WithName("GetTariffVersionById");
        group.MapPut(
                "/{tariffId:guid}/versions/{versionId:guid}/end",
                CloseVersionAsync)
            .WithName("CloseTariffVersion");

        endpoints.MapGet(
                "/api/v1/accounts/{accountId:guid}/tariff-assignments",
                ListAccountTariffAssignmentsAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Tariffs")
            .WithName("ListAccountTariffAssignments");

        endpoints.MapPut(
                "/api/v1/accounts/{accountId:guid}/tariff-assignments/{assignmentId:guid}/end",
                CloseAssignmentAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Tariffs")
            .WithName("CloseAccountTariffAssignment");

        endpoints.MapGet(
                "/api/v1/accounts/{accountId:guid}/tariff",
                GetAccountTariffAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Tariffs")
            .WithName("GetAccountEffectiveTariff");

        endpoints.MapPost(
                "/api/v1/accounts/{accountId:guid}/tariff-assignments",
                AssignToAccountAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Tariffs")
            .WithName("AssignTariffToAccount");

        return endpoints;
    }

    private static async Task<IResult> GetByIdAsync(
        Guid tariffId,
        HttpContext httpContext,
        GetTariffByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (tariffId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, TariffErrors.NotFound);
        }

        var result = await handler.Handle(
            new GetTariffByIdQuery(new TariffId(tariffId)),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetVersionByIdAsync(
        Guid tariffId,
        Guid versionId,
        HttpContext httpContext,
        GetTariffVersionByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (tariffId == Guid.Empty || versionId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, TariffErrors.VersionNotFound);
        }

        var result = await handler.Handle(
            new GetTariffVersionByIdQuery(new TariffVersionId(versionId)),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(httpContext, result.Error);
        }

        return result.Value.TariffId == tariffId
            ? Results.Ok(result.Value)
            : ApiProblemDetails.Create(httpContext, TariffErrors.VersionNotFound);
    }

    private static async Task<IResult> ListAccountTariffAssignmentsAsync(
        Guid accountId,
        HttpContext httpContext,
        ListAccountTariffAssignmentsHandler handler,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                AccountTariffAssignmentErrors.AccountNotFound);
        }

        var result = await handler.Handle(
            new ListAccountTariffAssignmentsQuery(new AccountId(accountId)),
            cancellationToken);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> CloseAssignmentAsync(
        Guid accountId,
        Guid assignmentId,
        CloseTariffAssignmentRequest request,
        HttpContext httpContext,
        CloseTariffAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || assignmentId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                AccountTariffAssignmentErrors.NotFound);
        }

        var result = await handler.Handle(
            new CloseTariffAssignmentCommand(
                new AccountId(accountId),
                new AccountTariffAssignmentId(assignmentId),
                request.EffectiveTo,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.NoContent();
    }

    private static async Task<IResult> GetAccountTariffAsync(
        Guid accountId,
        DateOnly? date,
        HttpContext httpContext,
        GetAccountTariffHandler handler,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                AccountTariffAssignmentErrors.AccountNotFound);
        }

        var billingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bishkek");
        var localNow = TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(),
            billingTimeZone);
        var effectiveDate = date ?? DateOnly.FromDateTime(localNow.DateTime);
        var result = await handler.Handle(
            new GetAccountTariffQuery(new AccountId(accountId), effectiveDate),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> CloseVersionAsync(
        Guid tariffId,
        Guid versionId,
        CloseTariffVersionRequest request,
        HttpContext httpContext,
        CloseTariffVersionHandler handler,
        CancellationToken cancellationToken)
    {
        if (tariffId == Guid.Empty || versionId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                TariffErrors.VersionNotFound);
        }

        var result = await handler.Handle(
            new CloseTariffVersionCommand(
                new TariffId(tariffId),
                new TariffVersionId(versionId),
                request.EffectiveTo,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.NoContent();
    }

    private static async Task<IResult> CreateAsync(
        CreateTariffRequest request,
        HttpContext httpContext,
        CreateTariffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateTariffCommand(
                request.Name,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Created(
                $"/api/v1/tariffs/{result.Value.Value:D}",
                new TariffMutationResponse(result.Value.Value, "created"));
    }

    private static async Task<IResult> CreateVersionAsync(
        Guid tariffId,
        CreateTariffVersionRequest request,
        HttpContext httpContext,
        CreateTariffVersionHandler handler,
        CancellationToken cancellationToken)
    {
        if (tariffId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, TariffErrors.NotFound);
        }

        var result = await handler.Handle(
            new CreateTariffVersionCommand(
                new TariffId(tariffId),
                request.Rate,
                request.EffectiveFrom,
                request.EffectiveTo,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Created(
                $"/api/v1/tariffs/{tariffId:D}/versions/{result.Value.Value:D}",
                new TariffVersionMutationResponse(result.Value.Value, "created"));
    }

    private static async Task<IResult> AssignToAccountAsync(
        Guid accountId,
        AssignTariffRequest request,
        HttpContext httpContext,
        AssignTariffToAccountHandler handler,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || request.TariffId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                AccountTariffAssignmentErrors.AccountNotFound);
        }

        var result = await handler.Handle(
            new AssignTariffToAccountCommand(
                new AccountId(accountId),
                new TariffId(request.TariffId),
                request.EffectiveFrom,
                request.EffectiveTo,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Created(
                $"/api/v1/accounts/{accountId:D}/tariff-assignments/{result.Value.AssignmentId.Value:D}",
                result.Value);
    }

    private static async Task<IResult> ListAsync(
        ListTariffsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ListVersionsAsync(
        Guid tariffId,
        HttpContext httpContext,
        ListTariffVersionsHandler handler,
        CancellationToken cancellationToken)
    {
        if (tariffId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, TariffErrors.NotFound);
        }

        var result = await handler.Handle(
            new ListTariffVersionsQuery(new TariffId(tariffId)),
            cancellationToken);
        return Results.Ok(result.Value);
    }
}

public sealed record CreateTariffRequest(string? Name);

public sealed record CreateTariffVersionRequest(
    decimal Rate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record AssignTariffRequest(
    Guid TariffId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record TariffMutationResponse(Guid TariffId, string Status);

public sealed record TariffVersionMutationResponse(
    Guid TariffVersionId,
    string Status);

public sealed record CloseTariffAssignmentRequest(DateOnly EffectiveTo);

public sealed record CloseTariffVersionRequest(DateOnly EffectiveTo);
