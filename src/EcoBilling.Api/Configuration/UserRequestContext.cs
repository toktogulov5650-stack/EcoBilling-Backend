using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Api.Configuration;

public static class UserRequestContext
{
    public static UserId GetUserId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var subject = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Authenticated user is missing a valid subject claim.");
        }

        return new UserId(userId);
    }

    public static UserRole GetRole(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var roleValue = httpContext.User.FindFirst("role")?.Value;
        if (!Enum.TryParse<UserRole>(roleValue, ignoreCase: false, out var role) ||
            !Enum.IsDefined(role))
        {
            throw new InvalidOperationException(
                "Authenticated user is missing a valid role claim.");
        }

        return role;
    }
}
