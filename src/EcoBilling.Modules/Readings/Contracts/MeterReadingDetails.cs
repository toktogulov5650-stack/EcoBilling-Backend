namespace EcoBilling.Modules.Readings.Contracts;

public sealed record MeterReadingDetails(
    Guid Id,
    Guid MeterId,
    decimal Value,
    DateTimeOffset MeasuredAt,
    DateTimeOffset CreatedAt);
