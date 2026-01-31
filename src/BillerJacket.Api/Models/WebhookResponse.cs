namespace BillerJacket.Api.Models;

public record WebhookResponse(
    Guid WebhookEventId,
    string Status);
