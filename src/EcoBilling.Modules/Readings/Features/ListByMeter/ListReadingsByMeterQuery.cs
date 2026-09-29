using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.Modules.Readings.Features.ListByMeter;

public sealed record ListReadingsByMeterQuery(MeterId MeterId);
