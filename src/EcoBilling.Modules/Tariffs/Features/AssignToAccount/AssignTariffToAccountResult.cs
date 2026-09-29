using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.AssignToAccount;

public sealed record AssignTariffToAccountResult(
    AccountTariffAssignmentId AssignmentId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);
