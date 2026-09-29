using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Tariffs.Features.GetForAccount;

public sealed record GetAccountTariffQuery(AccountId AccountId, DateOnly Date);
