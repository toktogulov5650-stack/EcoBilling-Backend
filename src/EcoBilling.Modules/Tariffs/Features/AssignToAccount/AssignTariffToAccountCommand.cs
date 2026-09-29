using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.AssignToAccount;

public sealed record AssignTariffToAccountCommand(
    AccountId AccountId,
    TariffId TariffId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string ActorId,
    string CorrelationId);
