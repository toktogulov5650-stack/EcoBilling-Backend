using EcoBilling.Modules.Tariffs.Contracts;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.ListVersions;

public sealed class ListTariffVersionsHandler(ITariffVersionRepository repository)
{
    public async Task<Result<IReadOnlyList<TariffVersionDetails>>> Handle(
        ListTariffVersionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.TariffId);

        var versions = await repository.ListByTariffIdAsync(
            query.TariffId,
            cancellationToken);

        return Result<IReadOnlyList<TariffVersionDetails>>.Success(
            versions.Select(
                version => new TariffVersionDetails(
                    version.Id.Value,
                    version.TariffId.Value,
                    version.Rate.Value,
                    version.EffectiveFrom,
                    version.EffectiveTo,
                    version.CreatedAt))
                .ToArray());
    }
}
