using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Readings.Domain;

public sealed class MeterReading
{
    public const int MaximumCorrectionReasonLength = 500;

    private MeterReading()
    {
        Id = null!;
        MeterId = null!;
        Value = null!;
    }

    private MeterReading(
        MeterReadingId id,
        MeterId meterId,
        ReadingValue value,
        DateTimeOffset measuredAt,
        DateTimeOffset createdAt,
        UserId? authorUserId,
        ReadingSource source,
        MeterReadingId? supersedesReadingId,
        string? correctionReason)
    {
        Id = id;
        MeterId = meterId;
        Value = value;
        MeasuredAt = measuredAt.ToUniversalTime();
        CreatedAt = createdAt.ToUniversalTime();
        AuthorUserId = authorUserId;
        Source = source;
        SupersedesReadingId = supersedesReadingId;
        CorrectionReason = correctionReason;
    }

    public MeterReadingId Id { get; private set; }

    public MeterId MeterId { get; private set; }

    public ReadingValue Value { get; private set; }

    public DateTimeOffset MeasuredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public UserId? AuthorUserId { get; private set; }

    public ReadingSource Source { get; private set; }

    public MeterReadingId? SupersedesReadingId { get; private set; }

    public string? CorrectionReason { get; private set; }

    public static Result<MeterReading> Create(
        MeterReadingId id,
        MeterId meterId,
        decimal value,
        DateTimeOffset measuredAt,
        DateTimeOffset createdAt) =>
        Create(
            id,
            meterId,
            value,
            measuredAt,
            createdAt,
            authorUserId: null,
            ReadingSource.Import,
            supersedesReadingId: null,
            correctionReason: null);

    public static Result<MeterReading> Create(
        MeterReadingId id,
        MeterId meterId,
        decimal value,
        DateTimeOffset measuredAt,
        DateTimeOffset createdAt,
        UserId? authorUserId,
        ReadingSource source,
        MeterReadingId? supersedesReadingId,
        string? correctionReason)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(meterId);

        var readingValue = ReadingValue.Create(value);
        if (readingValue.IsFailure)
        {
            return Result<MeterReading>.Failure(readingValue.Error);
        }

        if (!Enum.IsDefined(source))
        {
            return Result<MeterReading>.Failure(MeterReadingErrors.InvalidSource);
        }

        var normalizedReason = string.IsNullOrWhiteSpace(correctionReason)
            ? null
            : correctionReason.Trim();

        if (normalizedReason?.Length > MaximumCorrectionReasonLength)
        {
            return Result<MeterReading>.Failure(
                MeterReadingErrors.InvalidCorrectionReason);
        }

        if (source is ReadingSource.Correction &&
            (supersedesReadingId is null || normalizedReason is null))
        {
            return Result<MeterReading>.Failure(
                MeterReadingErrors.InvalidCorrection);
        }

        if (source is not ReadingSource.Correction &&
            supersedesReadingId is not null)
        {
            return Result<MeterReading>.Failure(
                MeterReadingErrors.InvalidCorrection);
        }

        return Result<MeterReading>.Success(
            new MeterReading(
                id,
                meterId,
                readingValue.Value,
                measuredAt,
                createdAt,
                authorUserId,
                source,
                supersedesReadingId,
                normalizedReason));
    }
}
