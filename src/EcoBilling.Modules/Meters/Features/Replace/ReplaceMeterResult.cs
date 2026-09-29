using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.Modules.Meters.Features.Replace;

public sealed record ReplaceMeterResult(
    MeterId RetiredMeterId,
    MeterId ReplacementMeterId);
