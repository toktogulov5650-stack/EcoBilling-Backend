using EcoBilling.Modules.Tariffs.Contracts;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.GetVersionById;

public sealed class GetTariffVersionByIdHandler(
    ITariffVersionRepository tariffVersionRepository)
{
    public async Task<Result<TariffVersionDetails>> Handle(
        GetTariffVersionByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.TariffVersionId);

        var version = await tariffVersionRepository.GetByIdAsync(
            query.TariffVersionId,
            cancellationToken);

        if (version is null)
        {
            return Result<TariffVersionDetails>.Failure(TariffErrors.VersionNotFound);
        }

        return Result<TariffVersionDetails>.Success(
            new TariffVersionDetails(
                version.Id.Value,
                version.TariffId.Value,
                version.Rate.Value,
                version.EffectiveFrom,
                version.EffectiveTo,
                version.CreatedAt));
    }
}
