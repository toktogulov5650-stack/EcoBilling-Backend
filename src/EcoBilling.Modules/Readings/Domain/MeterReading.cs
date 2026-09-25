using EcoBilling.Modules.Meters.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Readings.Domain;

public sealed class MeterReading
{
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
        DateTimeOffset createdAt)
    {
        Id = id;
        MeterId = meterId;
        Value = value;
        MeasuredAt = measuredAt;
        CreatedAt = createdAt;
    }

    public MeterReadingId Id { get; private set; }

    public MeterId MeterId { get; private set; }

    public ReadingValue Value { get; private set; }

    public DateTimeOffset MeasuredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<MeterReading> Create(
        MeterReadingId id,
        MeterId meterId,
        decimal value,
        DateTimeOffset measuredAt,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(meterId);

        var readingValue = ReadingValue.Create(value);
        if (readingValue.IsFailure)
        {
            return Result<MeterReading>.Failure(readingValue.Error);
        }

        return Result<MeterReading>.Success(
            new MeterReading(
                id,
                meterId,
                readingValue.Value,
                measuredAt.ToUniversalTime(),
                createdAt.ToUniversalTime()));
    }
}
