using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Billing.Features.ListByAccount;

public sealed record ListChargesByAccountQuery(AccountId AccountId);
