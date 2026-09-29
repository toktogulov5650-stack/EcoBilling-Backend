using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.GetAddressById;
using EcoBilling.Modules.Accounts.Features.GetByNumber;

namespace EcoBilling.Api.Endpoints;

public static class AccountManagementEndpoints
{
    public static IEndpointRouteBuilder MapAccountManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/accounts/by-number/{accountNumber}",
                GetByNumberAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Accounts")
            .WithName("GetAccountByNumber");

        endpoints.MapGet(
                "/api/v1/addresses/{addressId:guid}",
                GetAddressByIdAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Accounts")
            .WithName("GetAddressById");

        return endpoints;
    }

    private static async Task<IResult> GetByNumberAsync(
        string accountNumber,
        HttpContext httpContext,
        GetAccountByNumberHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetAccountByNumberQuery(accountNumber),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAddressByIdAsync(
        Guid addressId,
        HttpContext httpContext,
        GetAddressByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (addressId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, AddressErrors.NotFound);
        }

        var result = await handler.Handle(
            new GetAddressByIdQuery(new AddressId(addressId)),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(result.Value);
    }
}
