using EcoBilling.Modules.Reports.Contracts;

namespace EcoBilling.Modules.Reports.Features.Abstractions;

public interface IDistrictOperationalSummaryReader
{
    Task<DistrictOperationalSummary> ReadAsync(
        CancellationToken cancellationToken);
}
