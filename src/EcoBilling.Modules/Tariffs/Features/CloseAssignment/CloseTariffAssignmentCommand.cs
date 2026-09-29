using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.CloseAssignment;

public sealed record CloseTariffAssignmentCommand(
    AccountId AccountId,
    AccountTariffAssignmentId AssignmentId,
    DateOnly EffectiveTo,
    string ActorId,
    string CorrelationId);
