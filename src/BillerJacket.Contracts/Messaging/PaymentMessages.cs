namespace BillerJacket.Contracts.Messaging;

public sealed record ApplyPaymentCommand(
    string TenantId,
    string CorrelationId,
    string InvoiceId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "payment.apply";
}
