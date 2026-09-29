using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Payments.Features.GetFinancialSummary;
using EcoBilling.Modules.Payments.Features.ListByAccount;
using EcoBilling.Modules.Payments.Features.RegisterManual;

namespace EcoBilling.Api.Endpoints;

public static class PaymentEndpoints
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapPaymentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/v1/accounts/{accountId:guid}/payments",
                RegisterAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Payments")
            .WithName("RegisterManualPayment");

        endpoints.MapGet(
                "/api/v1/accounts/{accountId:guid}/payments",
                ListAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Payments")
            .WithName("ListAccountPayments");

        endpoints.MapGet(
                "/api/v1/accounts/{accountId:guid}/financial",
                GetFinancialAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Payments")
            .WithName("GetAccountFinancialSummary");

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        Guid accountId,
        RegisterManualPaymentRequest request,
        HttpContext httpContext,
        RegisterManualPaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterManualPaymentCommand(
                new AccountId(accountId),
                request.Amount,
                httpContext.Request.Headers[IdempotencyKeyHeaderName].ToString(),
                request.PaidAt,
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
        ListPaymentsByAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ListPaymentsByAccountQuery(new AccountId(accountId)),
            cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetFinancialAsync(
        Guid accountId,
        HttpContext httpContext,
        GetAccountFinancialSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetAccountFinancialSummaryQuery(new AccountId(accountId)),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }
}

public sealed record RegisterManualPaymentRequest(
    decimal Amount,
    DateTimeOffset PaidAt);
