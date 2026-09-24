using EcoBilling.Modules.Identity.Contracts;

namespace EcoBilling.Modules.Identity.Application.Authenticate;

public sealed record AuthenticationResult(AuthenticatedUser User);
