using EcoBilling.Modules.Tariffs.Contracts;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.GetById;

public sealed class GetTariffByIdHandler(ITariffRepository tariffRepository)
{
    public async Task<Result<TariffDetails>> Handle(
        GetTariffByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.TariffId);

        var tariff = await tariffRepository.GetByIdAsync(
            query.TariffId,
            cancellationToken);

        if (tariff is null)
        {
            return Result<TariffDetails>.Failure(TariffErrors.NotFound);
        }

        return Result<TariffDetails>.Success(
            new TariffDetails(
                tariff.Id.Value,
                tariff.Name.Value,
                tariff.CreatedAt));
    }
}
