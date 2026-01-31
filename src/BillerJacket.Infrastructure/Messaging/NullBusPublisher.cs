using BillerJacket.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace BillerJacket.Infrastructure.Messaging;

public sealed class NullBusPublisher : IBusPublisher
{
    private readonly ILogger<NullBusPublisher> _logger;

    public NullBusPublisher(ILogger<NullBusPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string queueName, IMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Service Bus not configured. Dropping {MessageType} to {Queue} (tenant={TenantId}, corr={CorrelationId})",
            message.MessageType, queueName, message.TenantId, message.CorrelationId);

        return Task.CompletedTask;
    }
}
