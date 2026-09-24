using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Contracts;

public sealed record AuthenticatedUser(UserId UserId, UserRole Role);
