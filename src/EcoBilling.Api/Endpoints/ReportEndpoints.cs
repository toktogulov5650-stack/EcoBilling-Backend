using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Reports.Features.GetFinancialSummary;
using EcoBilling.Modules.Reports.Features.GetOperationalSummary;

namespace EcoBilling.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reports")
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Reports");

        group.MapGet("/operational-summary", GetOperationalAsync)
            .WithName("GetOperationalSummary");
        group.MapGet("/financial-summary", GetFinancialAsync)
            .WithName("GetFinancialSummary");

        return endpoints;
    }

    private static async Task<IResult> GetOperationalAsync(
        GetDistrictOperationalSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(cancellationToken);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetFinancialAsync(
        GetDistrictFinancialSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(cancellationToken);
        return Results.Ok(result.Value);
    }
}
