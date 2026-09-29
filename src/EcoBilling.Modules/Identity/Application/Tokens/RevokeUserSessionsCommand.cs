using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed record RevokeUserSessionsCommand(
    UserId UserId,
    string ActorId,
    string CorrelationId);
