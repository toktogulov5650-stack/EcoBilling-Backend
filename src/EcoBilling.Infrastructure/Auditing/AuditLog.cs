using System.Text.Json;

namespace EcoBilling.Infrastructure.Auditing;

public sealed class AuditLog
{
    public const int MaximumActorTypeLength = 64;
    public const int MaximumActorIdLength = 200;
    public const int MaximumActionLength = 200;
    public const int MaximumEntityTypeLength = 200;
    public const int MaximumEntityIdLength = 200;
    public const int MaximumCorrelationIdLength = 200;

    private AuditLog()
    {
        ActorType = string.Empty;
        ActorId = string.Empty;
        Action = string.Empty;
        EntityType = string.Empty;
        EntityId = string.Empty;
        CorrelationId = string.Empty;
    }

    public AuditLog(
        Guid id,
        string actorType,
        string actorId,
        string action,
        string entityType,
        string entityId,
        string? beforeData,
        string? afterData,
        string correlationId,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Audit log identifier cannot be empty.", nameof(id));
        }

        ValidateRequired(actorType, MaximumActorTypeLength, nameof(actorType));
        ValidateRequired(actorId, MaximumActorIdLength, nameof(actorId));
        ValidateRequired(action, MaximumActionLength, nameof(action));
        ValidateRequired(entityType, MaximumEntityTypeLength, nameof(entityType));
        ValidateRequired(entityId, MaximumEntityIdLength, nameof(entityId));
        ValidateRequired(
            correlationId,
            MaximumCorrelationIdLength,
            nameof(correlationId));
        ValidateJson(beforeData, nameof(beforeData));
        ValidateJson(afterData, nameof(afterData));

        Id = id;
        ActorType = actorType;
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeData = beforeData;
        AfterData = afterData;
        CorrelationId = correlationId;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string ActorType { get; private set; }
    public string ActorId { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }
    public string? BeforeData { get; private set; }
    public string? AfterData { get; private set; }
    public string CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private static void ValidateRequired(
        string value,
        int maximumLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateJson(string? value, string parameterName)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Audit data must contain valid JSON.",
                parameterName,
                exception);
        }
    }
}
