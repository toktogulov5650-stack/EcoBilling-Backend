using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Payments.Features.ListByAccount;

public sealed record ListPaymentsByAccountQuery(AccountId AccountId);
