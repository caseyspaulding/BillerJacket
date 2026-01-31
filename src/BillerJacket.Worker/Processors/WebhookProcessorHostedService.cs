using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Worker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillerJacket.Worker.Processors;

public class WebhookProcessorHostedService : QueueProcessorBase
{
    public WebhookProcessorHostedService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookProcessorHostedService> logger)
        : base(client, scopeFactory, logger, Queues.WebhookIngest)
    {
    }

    protected override async Task HandleAsync(MessageEnvelope envelope, IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<ArDbContext>();
        var logger = scopedServices.GetRequiredService<ILogger<WebhookProcessorHostedService>>();
        var logging = new LoggingContext(logger);

        var tenantId = Guid.TryParse(envelope.TenantId, out var tid) ? tid : (Guid?)null;

        using var _ = logging.WithContext(
            feature: "Webhook",
            operation: "ProcessWebhook",
            component: "Worker",
            tenantId: tenantId);

        if (!tenantId.HasValue)
            throw new DeadLetterException("Missing TenantId");

        switch (envelope.MessageType)
        {
            case "webhook.received":
            {
                var msg = JsonSerializer.Deserialize<WebhookReceived>(envelope.PayloadJson, JsonDefaults.Options)
                    ?? throw new DeadLetterException("Failed to deserialize WebhookReceived");

                if (!Guid.TryParse(msg.WebhookEventId, out var eventId))
                    throw new DeadLetterException($"Invalid WebhookEventId: {msg.WebhookEventId}");

                var webhookEvent = await db.WebhookEvents.FirstOrDefaultAsync(w => w.WebhookEventId == eventId, ct);
                if (webhookEvent is null)
                    throw new DeadLetterException($"WebhookEvent not found: {msg.WebhookEventId}");

                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Processed;
                webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;

                db.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    EntityType = "Webhook",
                    EntityId = eventId.ToString(),
                    Action = "webhook.processed",
                    DataJson = JsonSerializer.Serialize(new { msg.Provider, webhookEvent.EventType }),
                    PerformedByUserId = SystemUser.Id,
                    OccurredAt = DateTimeOffset.UtcNow,
                    CorrelationId = envelope.CorrelationId
                });

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Processed webhook {EventId} from {Provider}", eventId, msg.Provider);
                break;
            }

            case "webhook.replay_requested":
            {
                var msg = JsonSerializer.Deserialize<WebhookReplayRequested>(envelope.PayloadJson, JsonDefaults.Options)
                    ?? throw new DeadLetterException("Failed to deserialize WebhookReplayRequested");

                if (!Guid.TryParse(msg.WebhookEventId, out var eventId))
                    throw new DeadLetterException($"Invalid WebhookEventId: {msg.WebhookEventId}");

                var webhookEvent = await db.WebhookEvents.FirstOrDefaultAsync(w => w.WebhookEventId == eventId, ct);
                if (webhookEvent is null)
                    throw new DeadLetterException($"WebhookEvent not found: {msg.WebhookEventId}");

                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Processed;
                webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;

                db.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    EntityType = "Webhook",
                    EntityId = eventId.ToString(),
                    Action = "webhook.replayed",
                    DataJson = JsonSerializer.Serialize(new { webhookEvent.Provider, webhookEvent.EventType }),
                    PerformedByUserId = SystemUser.Id,
                    OccurredAt = DateTimeOffset.UtcNow,
                    CorrelationId = envelope.CorrelationId
                });

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Replayed webhook {EventId}", eventId);
                break;
            }

            default:
                throw new DeadLetterException($"Unknown message type: {envelope.MessageType}");
        }
    }
}
