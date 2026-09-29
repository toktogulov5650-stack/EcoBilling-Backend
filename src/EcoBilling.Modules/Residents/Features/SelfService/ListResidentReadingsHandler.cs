using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.SelfService;

public sealed class ListResidentReadingsHandler(IResidentSelfServiceReader reader)
{
    public async Task<Result<IReadOnlyList<ResidentReadingView>>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return Result<IReadOnlyList<ResidentReadingView>>.Success(
            await reader.ListReadingsAsync(userId, cancellationToken));
    }
}
