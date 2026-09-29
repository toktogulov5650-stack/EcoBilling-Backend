using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.Modules.Meters.Features.Abstractions;

public sealed record MeterPersistenceResult(
    MeterPersistenceOutcome Outcome,
    MeterId? MeterId = null);

public enum MeterPersistenceOutcome
{
    Created = 0,
    Replaced = 1,
    AccountNotFound = 2,
    MeterNotFound = 3,
    SerialNumberAlreadyExists = 4,
    AlreadyRetired = 5,
    ReplacementAccountMismatch = 6
}
