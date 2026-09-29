namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentReadingView(
    Guid ReadingId,
    Guid MeterId,
    decimal Value,
    DateTimeOffset MeasuredAt,
    string Source);
