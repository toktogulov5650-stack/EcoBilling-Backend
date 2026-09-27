using System.Text.Json;

namespace EcoBilling.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public const int MaximumTypeLength = 200;
    public const int MaximumLastErrorLength = 2000;

    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    public OutboxMessage(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Outbox message identifier cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        if (type.Length > MaximumTypeLength)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        try
        {
            using var _ = JsonDocument.Parse(payload);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Outbox payload must contain valid JSON.",
                nameof(payload),
                exception);
        }

        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (ProcessedAt is not null)
        {
            throw new InvalidOperationException("Outbox message is already processed.");
        }

        var utcProcessedAt = processedAt.ToUniversalTime();
        if (utcProcessedAt < OccurredAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processedAt),
                "Processed time cannot precede the event occurrence.");
        }

        ProcessedAt = utcProcessedAt;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        if (error.Length > MaximumLastErrorLength)
        {
            throw new ArgumentOutOfRangeException(nameof(error));
        }

        if (ProcessedAt is not null)
        {
            throw new InvalidOperationException(
                "A processed outbox message cannot record a failure.");
        }

        RetryCount = checked(RetryCount + 1);
        LastError = error;
    }
}
