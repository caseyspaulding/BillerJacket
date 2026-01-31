using BillerJacket.Contracts.Messaging;

namespace BillerJacket.Infrastructure.Messaging;

public interface IBusPublisher
{
    Task PublishAsync(string queueName, IMessage message, CancellationToken ct = default);
}
