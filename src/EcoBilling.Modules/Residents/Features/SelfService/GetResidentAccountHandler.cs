using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.SelfService;

public sealed class GetResidentAccountHandler(IResidentSelfServiceReader reader)
{
    public async Task<Result<ResidentAccountView>> Handle(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var view = await reader.GetAccountAsync(userId, cancellationToken);
        return view is null
            ? Result<ResidentAccountView>.Failure(ResidentErrors.NotFound)
            : Result<ResidentAccountView>.Success(view);
    }
}
