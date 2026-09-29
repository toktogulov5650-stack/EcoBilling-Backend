using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentSelfServiceReader
{
    Task<ResidentAccountView?> GetAccountAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResidentMeterView>> ListMetersAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResidentReadingView>> ListReadingsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResidentChargeView>> ListChargesAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResidentPaymentView>> ListPaymentsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<ResidentFinancialView?> GetFinancialAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
