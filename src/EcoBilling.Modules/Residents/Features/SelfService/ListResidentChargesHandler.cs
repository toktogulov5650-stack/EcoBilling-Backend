using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.SelfService;

public sealed class ListResidentChargesHandler(IResidentSelfServiceReader reader)
{
    public async Task<Result<IReadOnlyList<ResidentChargeView>>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return Result<IReadOnlyList<ResidentChargeView>>.Success(
            await reader.ListChargesAsync(userId, cancellationToken));
    }
}
