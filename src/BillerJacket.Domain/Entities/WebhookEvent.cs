using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class WebhookEvent
{
    public Guid WebhookEventId { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ExternalEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public WebhookProcessingStatus ProcessingStatus { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}
