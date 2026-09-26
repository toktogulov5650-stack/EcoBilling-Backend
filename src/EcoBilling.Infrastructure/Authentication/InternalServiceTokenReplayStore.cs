using EcoBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class InternalServiceTokenReplayStore(EcoBillingDbContext dbContext)
    : IInternalServiceTokenReplayStore
{
    public async Task<bool> TryConsumeAsync(
        string issuer,
        string tokenId,
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var replay = new InternalServiceTokenReplay(issuer, tokenId, expiresAt);

        await dbContext.InternalServiceTokenReplays
            .Where(existing => existing.ExpiresAt <= utcNow.ToUniversalTime())
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.InternalServiceTokenReplays.Add(replay);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return false;
        }
    }
}
