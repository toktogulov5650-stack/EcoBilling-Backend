using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class DirectorProfileReader(EcoBillingDbContext dbContext)
    : IDirectorProfileReader
{
    public async Task<DirectorProfileDetails?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var director = await dbContext.Directors
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId,
                cancellationToken);
        if (director is null)
        {
            return null;
        }

        var user = await dbContext.UserAccounts
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == director.UserId,
                cancellationToken);

        return new DirectorProfileDetails(
            director.Id.Value,
            director.UserId.Value,
            director.FullName,
            user.LoginIdentity.NormalizedValue,
            director.CreatedAt);
    }
}
