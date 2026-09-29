namespace EcoBilling.Infrastructure.Outbox;

public interface IOutboxMessagePublisher
{
    bool CanPublish(string messageType);

    Task PublishAsync(
        string messageType,
        string payload,
        CancellationToken cancellationToken);
}
