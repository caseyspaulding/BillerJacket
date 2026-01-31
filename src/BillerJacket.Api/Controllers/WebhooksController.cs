using System.Text.Json;
using BillerJacket.Infrastructure.Messaging;
using BillerJacket.Api.Models;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize]
public class WebhooksController : ControllerBase
{
    private readonly ArDbContext _db;
    private readonly IBusPublisher _bus;
    private readonly LoggingContext _logging;

    public WebhooksController(ArDbContext db, IBusPublisher bus, ILogger<WebhooksController> logger)
    {
        _db = db;
        _bus = bus;
        _logging = new LoggingContext(logger);
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> Ingest(string provider, CancellationToken ct)
    {
        var tenantId = Current.TenantId;

        using var _ = _logging.WithContext(
            feature: "Webhook",
            operation: "Ingest",
            component: "API",
            tenantId: tenantId);

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var webhookEvent = new WebhookEvent
        {
            WebhookEventId = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = provider,
            EventType = "raw",
            PayloadJson = rawBody,
            ProcessingStatus = WebhookProcessingStatus.Received,
            ReceivedAt = DateTimeOffset.UtcNow,
            CorrelationId = Current.CorrelationId
        };

        _db.WebhookEvents.Add(webhookEvent);
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(Queues.WebhookIngest, new WebhookReceived(
            TenantId: tenantId.ToString(),
            CorrelationId: Current.CorrelationId,
            Provider: provider,
            WebhookEventId: webhookEvent.WebhookEventId.ToString(),
            ExternalSource: provider,
            ExternalReferenceId: null,
            RequestedByUserId: null,
            OccurredAt: DateTimeOffset.UtcNow), ct);

        return Accepted(new WebhookResponse(webhookEvent.WebhookEventId, "received"));
    }

    [HttpPost("{id:guid}/replay")]
    public async Task<IActionResult> Replay(Guid id, CancellationToken ct)
    {
        var tenantId = Current.TenantId;
        var correlationId = Current.CorrelationId;

        using var _ = _logging.WithContext(
            feature: "Webhook",
            operation: "Replay",
            component: "API",
            tenantId: tenantId);

        var webhookEvent = await _db.WebhookEvents
            .FirstOrDefaultAsync(w => w.WebhookEventId == id, ct);

        if (webhookEvent is null)
            return NotFound(new { error = "Webhook event not found." });

        if (webhookEvent.ProcessingStatus is not (WebhookProcessingStatus.Processed or WebhookProcessingStatus.Failed))
            return BadRequest(new { error = $"Cannot replay webhook with status {webhookEvent.ProcessingStatus}." });

        await _bus.PublishAsync(Queues.WebhookIngest, new WebhookReplayRequested(
            TenantId: tenantId.ToString(),
            CorrelationId: correlationId,
            WebhookEventId: id.ToString(),
            RequestedByUserId: Current.UserIdOrNull?.ToString(),
            OccurredAt: DateTimeOffset.UtcNow), ct);

        _db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = "Webhook",
            EntityId = id.ToString(),
            Action = "webhook.replay_requested",
            DataJson = JsonSerializer.Serialize(new { webhookEvent.Provider, webhookEvent.EventType }),
            PerformedByUserId = Current.UserIdOrNull,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId
        });

        await _db.SaveChangesAsync(ct);

        return Accepted(new WebhookResponse(id, "replay_queued"));
    }
}
