using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Worker.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillerJacket.Worker.Processors;

public class EmailProcessorHostedService : QueueProcessorBase
{
    public EmailProcessorHostedService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailProcessorHostedService> logger)
        : base(client, scopeFactory, logger, Queues.EmailSend)
    {
    }

    protected override async Task HandleAsync(MessageEnvelope envelope, IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<ArDbContext>();
        var logger = scopedServices.GetRequiredService<ILogger<EmailProcessorHostedService>>();
        var logging = new LoggingContext(logger);

        var tenantId = Guid.TryParse(envelope.TenantId, out var tid) ? tid : (Guid?)null;

        using var _ = logging.WithContext(
            feature: "Email",
            operation: "ProcessEmail",
            component: "Worker",
            tenantId: tenantId);

        switch (envelope.MessageType)
        {
            case "email.invoice_requested":
            {
                var msg = JsonSerializer.Deserialize<InvoiceEmailRequested>(envelope.PayloadJson, JsonDefaults.Options)
                    ?? throw new DeadLetterException("Failed to deserialize InvoiceEmailRequested");

                db.CommunicationLogs.Add(new CommunicationLog
                {
                    CommunicationLogId = Guid.NewGuid(),
                    TenantId = tenantId ?? throw new DeadLetterException("Missing TenantId"),
                    Channel = CommunicationChannel.Email,
                    Type = CommunicationType.Invoice,
                    Status = CommunicationStatus.Sent,
                    InvoiceId = Guid.TryParse(msg.InvoiceId, out var invId) ? invId : null,
                    ToAddress = msg.ToEmail,
                    Subject = msg.Subject,
                    Provider = "simulated",
                    SentAt = DateTimeOffset.UtcNow,
                    CorrelationId = envelope.CorrelationId
                });

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Simulated invoice email to {ToEmail} for invoice {InvoiceId}",
                    msg.ToEmail, msg.InvoiceId);
                break;
            }

            case "email.dunning_requested":
            {
                var msg = JsonSerializer.Deserialize<DunningEmailRequested>(envelope.PayloadJson, JsonDefaults.Options)
                    ?? throw new DeadLetterException("Failed to deserialize DunningEmailRequested");

                db.CommunicationLogs.Add(new CommunicationLog
                {
                    CommunicationLogId = Guid.NewGuid(),
                    TenantId = tenantId ?? throw new DeadLetterException("Missing TenantId"),
                    Channel = CommunicationChannel.Email,
                    Type = CommunicationType.Dunning,
                    Status = CommunicationStatus.Sent,
                    CustomerId = Guid.TryParse(msg.CustomerId, out var custId) ? custId : null,
                    InvoiceId = Guid.TryParse(msg.InvoiceId, out var invId2) ? invId2 : null,
                    ToAddress = msg.ToEmail,
                    Subject = msg.Subject,
                    Provider = "simulated",
                    SentAt = DateTimeOffset.UtcNow,
                    CorrelationId = envelope.CorrelationId
                });

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Simulated dunning email (step {Step}) to {ToEmail} for invoice {InvoiceId}",
                    msg.DunningStepNumber, msg.ToEmail, msg.InvoiceId);
                break;
            }

            default:
                throw new DeadLetterException($"Unknown message type: {envelope.MessageType}");
        }
    }
}
