namespace BillerJacket.Contracts.Messaging;

public sealed record EvaluateDunningCommand(
    string TenantId,
    string CorrelationId,
    DateOnly AsOfDate,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "dunning.evaluate";
}
