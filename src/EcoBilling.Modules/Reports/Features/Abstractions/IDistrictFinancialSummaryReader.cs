using EcoBilling.Modules.Reports.Contracts;

namespace EcoBilling.Modules.Reports.Features.Abstractions;

public interface IDistrictFinancialSummaryReader
{
    Task<DistrictFinancialSummary> ReadAsync(
        CancellationToken cancellationToken);
}
