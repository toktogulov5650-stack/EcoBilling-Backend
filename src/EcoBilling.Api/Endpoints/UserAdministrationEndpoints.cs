using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Identity.Application.Tokens;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Api.Endpoints;

public static class UserAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapUserAdministrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/v1/users/{userId:guid}/sessions/revoke-all",
                RevokeAllSessionsAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Identity administration")
            .WithName("RevokeAllUserSessions")
            .Produces<RevokeUserSessionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> RevokeAllSessionsAsync(
        Guid userId,
        HttpContext httpContext,
        RevokeUserSessionsHandler handler,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return ApiProblemDetails.Create(
                httpContext,
                IdentityErrors.UserNotFound);
        }

        var result = await handler.Handle(
            new RevokeUserSessionsCommand(
                new UserId(userId),
                UserRequestContext.GetUserId(httpContext).Value.ToString("D"),
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Ok(
                new RevokeUserSessionsResponse(
                    result.Value.UserId.Value,
                    result.Value.RevokedSessions));
    }
}

public sealed record RevokeUserSessionsResponse(
    Guid UserId,
    int RevokedSessions);
