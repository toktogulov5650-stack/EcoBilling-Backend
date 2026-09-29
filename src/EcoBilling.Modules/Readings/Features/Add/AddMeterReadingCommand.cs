using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.Modules.Readings.Features.Add;

public sealed record AddMeterReadingCommand(
    MeterId MeterId,
    decimal Value,
    DateTimeOffset MeasuredAt,
    UserId ActorUserId,
    UserRole ActorRole,
    ReadingSource Source,
    MeterReadingId? SupersedesReadingId,
    string? Reason,
    string CorrelationId);
