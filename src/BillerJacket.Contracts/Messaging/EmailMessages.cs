namespace BillerJacket.Contracts.Messaging;

public sealed record InvoiceEmailRequested(
    string TenantId,
    string CorrelationId,
    string InvoiceId,
    string ToEmail,
    string Subject,
    string Body,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "email.invoice_requested";
}

public sealed record DunningEmailRequested(
    string TenantId,
    string CorrelationId,
    string InvoiceId,
    string CustomerId,
    int DunningStepNumber,
    string ToEmail,
    string Subject,
    string Body,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "email.dunning_requested";
}
