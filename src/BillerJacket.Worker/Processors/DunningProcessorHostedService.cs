using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Messaging;
using BillerJacket.Worker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillerJacket.Worker.Processors;

public class DunningProcessorHostedService : QueueProcessorBase
{
    public DunningProcessorHostedService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<DunningProcessorHostedService> logger)
        : base(client, scopeFactory, logger, Queues.DunningEvaluate)
    {
    }

    protected override async Task HandleAsync(MessageEnvelope envelope, IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<ArDbContext>();
        var bus = scopedServices.GetRequiredService<IBusPublisher>();
        var logger = scopedServices.GetRequiredService<ILogger<DunningProcessorHostedService>>();
        var logging = new LoggingContext(logger);

        var tenantId = Guid.TryParse(envelope.TenantId, out var tid) ? tid : (Guid?)null;

        using var _ = logging.WithContext(
            feature: "Dunning",
            operation: "EvaluateDunning",
            component: "Worker",
            tenantId: tenantId);

        if (envelope.MessageType != "dunning.evaluate")
            throw new DeadLetterException($"Unknown message type: {envelope.MessageType}");

        var command = JsonSerializer.Deserialize<EvaluateDunningCommand>(envelope.PayloadJson, JsonDefaults.Options)
            ?? throw new DeadLetterException("Failed to deserialize EvaluateDunningCommand");

        if (!tenantId.HasValue)
            throw new DeadLetterException("Missing TenantId");

        var defaultPlan = await db.DunningPlans
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.IsDefault && p.IsActive, ct);

        if (defaultPlan is null)
        {
            logger.LogInformation("No default dunning plan found for tenant {TenantId}, skipping", tenantId);
            return;
        }

        var steps = defaultPlan.Steps.OrderBy(s => s.StepNumber).ToList();
        if (steps.Count == 0)
        {
            logger.LogInformation("Default dunning plan has no steps, skipping");
            return;
        }

        var overdueInvoices = await db.Invoices
            .Include(i => i.DunningState)
            .Include(i => i.Customer)
            .Where(i => i.Status == InvoiceStatus.Overdue)
            .ToListAsync(ct);

        var asOfDate = command.AsOfDate;
        var processed = 0;

        foreach (var invoice in overdueInvoices)
        {
            if (invoice.DunningState is null)
            {
                var firstStep = steps[0];
                var nextAction = invoice.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                    .AddDays(firstStep.DaysAfterDue);

                invoice.DunningState = new InvoiceDunningState
                {
                    InvoiceId = invoice.InvoiceId,
                    TenantId = tenantId.Value,
                    DunningPlanId = defaultPlan.DunningPlanId,
                    CurrentStepNumber = 1,
                    NextActionAt = new DateTimeOffset(nextAction, TimeSpan.Zero)
                };

                db.InvoiceDunningStates.Add(invoice.DunningState);
            }

            var state = invoice.DunningState;
            if (state.NextActionAt.HasValue &&
                DateOnly.FromDateTime(state.NextActionAt.Value.UtcDateTime) <= asOfDate)
            {
                var currentStep = steps.FirstOrDefault(s => s.StepNumber == state.CurrentStepNumber);
                if (currentStep is null) continue;

                await bus.PublishAsync(Queues.EmailSend, new DunningEmailRequested(
                    TenantId: tenantId.Value.ToString(),
                    CorrelationId: envelope.CorrelationId,
                    InvoiceId: invoice.InvoiceId.ToString(),
                    CustomerId: invoice.CustomerId.ToString(),
                    DunningStepNumber: currentStep.StepNumber,
                    ToEmail: invoice.Customer.Email,
                    Subject: $"Payment Reminder: Invoice {invoice.InvoiceNumber} (Step {currentStep.StepNumber})",
                    Body: $"This is a reminder that invoice {invoice.InvoiceNumber} for {invoice.BalanceDue:C} is overdue.",
                    ExternalSource: null,
                    ExternalReferenceId: null,
                    RequestedByUserId: null,
                    OccurredAt: DateTimeOffset.UtcNow), ct);

                state.LastActionAt = DateTimeOffset.UtcNow;

                var nextStep = steps.FirstOrDefault(s => s.StepNumber == state.CurrentStepNumber + 1);
                if (nextStep is not null)
                {
                    state.CurrentStepNumber = nextStep.StepNumber;
                    var nextAction = invoice.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                        .AddDays(nextStep.DaysAfterDue);
                    state.NextActionAt = new DateTimeOffset(nextAction, TimeSpan.Zero);
                }
                else
                {
                    state.NextActionAt = null;
                }

                db.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    EntityType = "Invoice",
                    EntityId = invoice.InvoiceId.ToString(),
                    Action = "dunning.step_executed",
                    DataJson = JsonSerializer.Serialize(new
                    {
                        invoice.InvoiceNumber,
                        Step = currentStep.StepNumber,
                        PlanId = defaultPlan.DunningPlanId
                    }),
                    PerformedByUserId = SystemUser.Id,
                    OccurredAt = DateTimeOffset.UtcNow,
                    CorrelationId = envelope.CorrelationId
                });

                processed++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Dunning evaluation complete. Processed {Count} invoices", processed);
    }
}
