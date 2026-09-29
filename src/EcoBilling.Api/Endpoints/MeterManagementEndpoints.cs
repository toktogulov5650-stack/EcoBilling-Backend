using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Create;
using EcoBilling.Modules.Meters.Features.ListByAccount;
using EcoBilling.Modules.Meters.Features.Replace;

namespace EcoBilling.Api.Endpoints;

public static class MeterManagementEndpoints
{
    public static IEndpointRouteBuilder MapMeterManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/accounts/{accountId:guid}/meters", CreateAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Meters")
            .WithName("CreateMeter");

        endpoints.MapPost("/api/v1/meters/{meterId:guid}/replace", ReplaceAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Meters")
            .WithName("ReplaceMeter");

        endpoints.MapGet("/api/v1/accounts/{accountId:guid}/meters", ListAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Meters")
            .WithName("ListMetersByAccount");

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid accountId,
        CreateMeterRequest request,
        HttpContext httpContext,
        CreateMeterHandler handler,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, MeterErrors.AccountNotFound);
        }

        var result = await handler.Handle(
            new CreateMeterCommand(
                new AccountId(accountId),
                request.SerialNumber,
                request.InstalledAt,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Created(
                $"/api/v1/meters/{result.Value.Value:D}",
                new MeterMutationResponse(result.Value.Value, "created"));
    }

    private static async Task<IResult> ReplaceAsync(
        Guid meterId,
        ReplaceMeterRequest request,
        HttpContext httpContext,
        ReplaceMeterHandler handler,
        CancellationToken cancellationToken)
    {
        if (meterId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, MeterErrors.NotFound);
        }

        var result = await handler.Handle(
            new ReplaceMeterCommand(
                new MeterId(meterId),
                request.SerialNumber,
                request.InstalledAt,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(
                new ReplaceMeterResponse(
                    result.Value.RetiredMeterId.Value,
                    result.Value.ReplacementMeterId.Value));
    }

    private static async Task<IResult> ListAsync(
        Guid accountId,
        HttpContext httpContext,
        ListMetersByAccountHandler handler,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, MeterErrors.AccountNotFound);
        }

        var result = await handler.Handle(
            new ListMetersByAccountQuery(new AccountId(accountId)),
            cancellationToken);

        return Results.Ok(result.Value);
    }
}

public sealed record CreateMeterRequest(
    string? SerialNumber,
    DateTimeOffset InstalledAt);

public sealed record ReplaceMeterRequest(
    string? SerialNumber,
    DateTimeOffset InstalledAt);

public sealed record MeterMutationResponse(Guid MeterId, string Status);

public sealed record ReplaceMeterResponse(
    Guid RetiredMeterId,
    Guid ReplacementMeterId);
