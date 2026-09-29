using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Billing.Features.CalculateMonthly;
using EcoBilling.Modules.Billing.Features.GetById;
using EcoBilling.Modules.Billing.Features.ListByAccount;

namespace EcoBilling.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/charges/{chargeId:guid}", GetByIdAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Billing")
            .WithName("GetChargeById")
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost(
                "/api/v1/accounts/{accountId:guid}/billing/{year:int}/{month:int}",
                CalculateAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Billing")
            .WithName("CalculateMonthlyCharge");

        endpoints.MapGet(
                "/api/v1/accounts/{accountId:guid}/charges",
                ListAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Billing")
            .WithName("ListAccountCharges");

        return endpoints;
    }

    private static async Task<IResult> GetByIdAsync(
        Guid chargeId,
        HttpContext httpContext,
        GetChargeByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (chargeId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, ChargeErrors.NotFound);
        }

        var result = await handler.Handle(
            new GetChargeByIdQuery(new ChargeId(chargeId)),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> CalculateAsync(
        Guid accountId,
        int year,
        int month,
        HttpContext httpContext,
        CalculateMonthlyChargeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CalculateMonthlyChargeCommand(
                new AccountId(accountId),
                year,
                month,
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiProblemDetails.Create(httpContext, result.Error);
        }

        httpContext.Response.Headers["Idempotency-Replayed"] =
            result.Value.IsReplay ? "true" : "false";

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ListAsync(
        Guid accountId,
        HttpContext httpContext,
        ListChargesByAccountHandler handler,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                EcoBilling.Modules.Accounts.Domain.AccountErrors.NotFound);
        }

        var result = await handler.Handle(
            new ListChargesByAccountQuery(new AccountId(accountId)),
            cancellationToken);
        return Results.Ok(result.Value);
    }
}
