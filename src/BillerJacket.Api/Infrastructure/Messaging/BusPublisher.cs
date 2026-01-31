using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Contracts.Messaging;

namespace BillerJacket.Api.Infrastructure.Messaging;

public interface IBusPublisher
{
    Task PublishAsync(string queueName, IMessage message, CancellationToken ct = default);
}

public sealed class BusPublisher : IBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<BusPublisher> _logger;

    public BusPublisher(ServiceBusClient client, ILogger<BusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync(string queueName, IMessage message, CancellationToken ct = default)
    {
        var sender = _client.CreateSender(queueName);

        var payloadJson = JsonSerializer.Serialize(message, message.GetType(), JsonDefaults.Options);

        var envelope = new MessageEnvelope(
            MessageType: message.MessageType,
            PayloadJson: payloadJson,
            TenantId: message.TenantId,
            CorrelationId: message.CorrelationId,
            EnqueuedAt: DateTimeOffset.UtcNow
        );

        var envelopeJson = JsonSerializer.Serialize(envelope, JsonDefaults.Options);

        var sbMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(envelopeJson))
        {
            ContentType = "application/json",
            CorrelationId = message.CorrelationId,
            Subject = message.MessageType,
            MessageId = Guid.NewGuid().ToString("N")
        };

        sbMessage.ApplicationProperties["tenantId"] = message.TenantId;
        sbMessage.ApplicationProperties["messageType"] = message.MessageType;

        if (!string.IsNullOrWhiteSpace(message.ExternalSource))
            sbMessage.ApplicationProperties["externalSource"] = message.ExternalSource!;
        if (!string.IsNullOrWhiteSpace(message.ExternalReferenceId))
            sbMessage.ApplicationProperties["externalReferenceId"] = message.ExternalReferenceId!;

        _logger.LogInformation(
            "Publishing {MessageType} to {Queue} (tenant={TenantId}, corr={CorrelationId})",
            message.MessageType, queueName, message.TenantId, message.CorrelationId);

        await sender.SendMessageAsync(sbMessage, ct);
    }
}
