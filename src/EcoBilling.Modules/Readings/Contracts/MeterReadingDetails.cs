namespace EcoBilling.Modules.Readings.Contracts;

public sealed record MeterReadingDetails(
    Guid Id,
    Guid MeterId,
    decimal Value,
    DateTimeOffset MeasuredAt,
    DateTimeOffset CreatedAt,
    Guid? AuthorUserId = null,
    string Source = "Import",
    Guid? SupersedesReadingId = null,
    string? CorrectionReason = null);
