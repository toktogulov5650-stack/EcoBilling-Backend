using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Identity.Application.GetDirectorProfile;

namespace EcoBilling.Api.Endpoints;

public static class DirectorSelfServiceEndpoints
{
    public static IEndpointRouteBuilder MapDirectorSelfServiceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/directors/me/profile", GetProfileAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Directors")
            .WithName("GetMyDirectorProfile")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        HttpContext httpContext,
        GetDirectorProfileHandler handler,
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
