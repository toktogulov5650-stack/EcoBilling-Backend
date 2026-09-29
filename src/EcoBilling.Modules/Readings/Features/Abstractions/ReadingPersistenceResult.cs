using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.Modules.Readings.Features.Abstractions;

public sealed record ReadingPersistenceResult(
    ReadingPersistenceOutcome Outcome,
    MeterReadingId? ReadingId = null);

public enum ReadingPersistenceOutcome
{
    Added = 0,
    MeterNotFound = 1,
    MeterInactive = 2,
    AccessDenied = 3,
    DecreasedValue = 4,
    BackdatedRequiresDirector = 5,
    BackdatedReasonRequired = 6,
    SupersededReadingNotFound = 7
}
