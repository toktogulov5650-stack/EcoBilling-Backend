using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Residents.Features.GetProfile;
using EcoBilling.Modules.Residents.Features.SelfService;

namespace EcoBilling.Api.Endpoints;

public static class ResidentSelfServiceEndpoints
{
    public static IEndpointRouteBuilder MapResidentSelfServiceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/me")
            .RequireAuthorization(UserAuthenticationOptions.ResidentPolicy)
            .WithTags("Resident self-service");

        group.MapGet("/profile", GetProfileAsync)
            .WithName("GetMyResidentProfile");

        group.MapGet("/account", GetAccountAsync)
            .WithName("GetMyResidentAccount");

        group.MapGet("/meters", ListMetersAsync)
            .WithName("ListMyResidentMeters");

        group.MapGet("/readings", ListReadingsAsync)
            .WithName("ListMyResidentReadings");

        group.MapGet("/charges", ListChargesAsync)
            .WithName("ListMyResidentCharges");

        group.MapGet("/payments", ListPaymentsAsync)
            .WithName("ListMyResidentPayments");

        group.MapGet("/financial", GetFinancialAsync)
            .WithName("GetMyResidentFinancialSummary");

        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        HttpContext httpContext,
        GetResidentProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetResidentProfileQuery(UserRequestContext.GetUserId(httpContext)),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAccountAsync(
        HttpContext httpContext,
        GetResidentAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            UserRequestContext.GetUserId(httpContext),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListMetersAsync(
        HttpContext httpContext,
        ListResidentMetersHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            UserRequestContext.GetUserId(httpContext),
            cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ListReadingsAsync(
        HttpContext httpContext,
        ListResidentReadingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            UserRequestContext.GetUserId(httpContext),
            cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ListChargesAsync(
        HttpContext httpContext,
        ListResidentChargesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            UserRequestContext.GetUserId(httpContext),
            cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ListPaymentsAsync(
        HttpContext httpContext,
        ListResidentPaymentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            UserRequestContext.GetUserId(httpContext),
            cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetFinancialAsync(
        HttpContext httpContext,
        GetResidentFinancialHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            UserRequestContext.GetUserId(httpContext),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }
}
