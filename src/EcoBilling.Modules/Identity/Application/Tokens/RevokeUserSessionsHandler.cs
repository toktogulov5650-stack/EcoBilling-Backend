using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed record RevokeUserSessionsResult(
    UserId UserId,
    int RevokedSessions);

public sealed class RevokeUserSessionsHandler
{
    private readonly IRefreshSessionRepository repository;
    private readonly TimeProvider timeProvider;

    public RevokeUserSessionsHandler(
        IRefreshSessionRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<RevokeUserSessionsResult>> Handle(
        RevokeUserSessionsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var persistence = await repository.RevokeAllForUserAsync(
            command.UserId,
            command.ActorId,
            command.CorrelationId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return persistence.Outcome switch
        {
            UserSessionRevocationOutcome.Revoked =>
                Result<RevokeUserSessionsResult>.Success(
                    new RevokeUserSessionsResult(
                        command.UserId,
                        persistence.RevokedSessions)),
            UserSessionRevocationOutcome.UserNotFound =>
                Result<RevokeUserSessionsResult>.Failure(
                    IdentityErrors.UserNotFound),
            _ => throw new InvalidOperationException(
                $"Unknown user session revocation outcome: {persistence.Outcome}.")
        };
    }
}
