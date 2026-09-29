using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.SelfService;

public sealed class ListResidentPaymentsHandler(IResidentSelfServiceReader reader)
{
    public async Task<Result<IReadOnlyList<ResidentPaymentView>>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return Result<IReadOnlyList<ResidentPaymentView>>.Success(
            await reader.ListPaymentsAsync(userId, cancellationToken));
    }
}
