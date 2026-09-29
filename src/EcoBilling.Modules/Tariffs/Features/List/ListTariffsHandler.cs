using EcoBilling.Modules.Tariffs.Contracts;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.List;

public sealed class ListTariffsHandler(ITariffRepository repository)
{
    public async Task<Result<IReadOnlyList<TariffDetails>>> Handle(
        CancellationToken cancellationToken)
    {
        var tariffs = await repository.ListAsync(cancellationToken);
        return Result<IReadOnlyList<TariffDetails>>.Success(
            tariffs.Select(
                tariff => new TariffDetails(
                    tariff.Id.Value,
                    tariff.Name.Value,
                    tariff.CreatedAt))
                .ToArray());
    }
}
