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

public class PaymentProcessorHostedService : QueueProcessorBase
{
    public PaymentProcessorHostedService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentProcessorHostedService> logger)
        : base(client, scopeFactory, logger, Queues.PaymentCommands)
    {
    }

    protected override async Task HandleAsync(MessageEnvelope envelope, IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<ArDbContext>();
        var logger = scopedServices.GetRequiredService<ILogger<PaymentProcessorHostedService>>();
        var logging = new LoggingContext(logger);

        var tenantId = Guid.TryParse(envelope.TenantId, out var tid) ? tid : (Guid?)null;

        using var _ = logging.WithContext(
            feature: "Payment",
            operation: "ApplyPayment",
            component: "Worker",
            tenantId: tenantId);

        if (envelope.MessageType != "payment.apply")
            throw new DeadLetterException($"Unknown message type: {envelope.MessageType}");

        var command = JsonSerializer.Deserialize<ApplyPaymentCommand>(envelope.PayloadJson, JsonDefaults.Options)
            ?? throw new DeadLetterException("Failed to deserialize ApplyPaymentCommand");

        if (!tenantId.HasValue)
            throw new DeadLetterException("Missing TenantId");

        var existingKey = await db.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.KeyValue == command.IdempotencyKey && k.Operation == "payment", ct);

        if (existingKey is not null)
        {
            logger.LogInformation("Idempotency key {Key} already processed, skipping", command.IdempotencyKey);
            return;
        }

        if (!Guid.TryParse(command.InvoiceId, out var invoiceId))
            throw new DeadLetterException($"Invalid InvoiceId: {command.InvoiceId}");

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, ct);
        if (invoice is null)
            throw new DeadLetterException($"Invoice not found: {command.InvoiceId}");

        if (invoice.Status is not (InvoiceStatus.Sent or InvoiceStatus.Overdue))
            throw new DeadLetterException($"Invoice status {invoice.Status} is not valid for payment");

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            TenantId = tenantId.Value,
            InvoiceId = invoiceId,
            Amount = command.Amount,
            CurrencyCode = command.Currency,
            Method = PaymentMethod.External,
            Status = PaymentStatus.Succeeded,
            AppliedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = SystemUser.Id,
            CorrelationId = envelope.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Payments.Add(payment);

        invoice.PaidAmount += command.Amount;
        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTimeOffset.UtcNow;
        }

        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            IdempotencyKeyId = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Operation = "payment",
            KeyValue = command.IdempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TenantId = tenantId.Value,
            EntityType = "Payment",
            EntityId = payment.PaymentId.ToString(),
            Action = "payment.applied",
            DataJson = JsonSerializer.Serialize(new
            {
                payment.InvoiceId,
                payment.Amount,
                payment.CurrencyCode,
                InvoiceStatus = invoice.Status.ToString()
            }),
            PerformedByUserId = SystemUser.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = envelope.CorrelationId
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Applied payment of {Amount} {Currency} to invoice {InvoiceId}",
            command.Amount, command.Currency, command.InvoiceId);
    }
}
