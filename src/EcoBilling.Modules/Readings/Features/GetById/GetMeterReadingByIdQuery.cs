using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.Modules.Readings.Features.GetById;

public sealed record GetMeterReadingByIdQuery(MeterReadingId ReadingId);
