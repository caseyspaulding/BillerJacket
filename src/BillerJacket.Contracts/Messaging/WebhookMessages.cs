namespace BillerJacket.Contracts.Messaging;

public sealed record WebhookReceived(
    string TenantId,
    string CorrelationId,
    string Provider,
    string WebhookEventId,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "webhook.received";
}

public sealed record WebhookReplayRequested(
    string TenantId,
    string CorrelationId,
    string WebhookEventId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId,
    ExternalSource: null, ExternalReferenceId: null,
    RequestedByUserId, OccurredAt)
{
    public override string MessageType => "webhook.replay_requested";
}
