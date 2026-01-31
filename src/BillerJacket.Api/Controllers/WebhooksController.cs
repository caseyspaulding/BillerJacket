using BillerJacket.Api.Infrastructure.Messaging;
using BillerJacket.Api.Models;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
