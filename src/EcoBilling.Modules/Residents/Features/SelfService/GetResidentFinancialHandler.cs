using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.SelfService;

public sealed class GetResidentFinancialHandler(IResidentSelfServiceReader reader)
{
    public async Task<Result<ResidentFinancialView>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var view = await reader.GetFinancialAsync(userId, cancellationToken);
        return view is null
            ? Result<ResidentFinancialView>.Failure(ResidentErrors.NotFound)
            : Result<ResidentFinancialView>.Success(view);
    }
}
