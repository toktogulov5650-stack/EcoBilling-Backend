using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.Modules.Meters.Features.Replace;

public sealed record ReplaceMeterCommand(
    MeterId MeterId,
    string? NewSerialNumber,
    DateTimeOffset InstalledAt,
    string ActorId,
    string CorrelationId);
