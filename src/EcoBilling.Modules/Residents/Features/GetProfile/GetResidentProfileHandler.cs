using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.GetProfile;

public sealed class GetResidentProfileHandler(IResidentRepository residentRepository)
{
    public async Task<Result<ResidentProfile>> Handle(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.UserId);

        var resident = await residentRepository.GetByUserIdAsync(
            query.UserId,
            cancellationToken);

        if (resident is null)
        {
            return Result<ResidentProfile>.Failure(ResidentErrors.NotFound);
        }

        return Result<ResidentProfile>.Success(
            new ResidentProfile(
                resident.Id.Value,
                resident.UserId.Value,
                resident.FullName,
                resident.CreatedAt));
    }
}
