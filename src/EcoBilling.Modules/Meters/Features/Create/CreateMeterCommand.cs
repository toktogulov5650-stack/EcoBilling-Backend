using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.Modules.Meters.Features.Create;

public sealed record CreateMeterCommand(
    AccountId AccountId,
    string? SerialNumber,
    DateTimeOffset InstalledAt,
    string ActorId,
    string CorrelationId);
