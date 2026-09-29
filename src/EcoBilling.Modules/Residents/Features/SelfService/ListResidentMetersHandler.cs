using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.SelfService;

public sealed class ListResidentMetersHandler(IResidentSelfServiceReader reader)
{
    public async Task<Result<IReadOnlyList<ResidentMeterView>>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return Result<IReadOnlyList<ResidentMeterView>>.Success(
            await reader.ListMetersAsync(userId, cancellationToken));
    }
}
