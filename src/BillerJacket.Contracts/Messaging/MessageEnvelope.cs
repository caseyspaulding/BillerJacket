namespace BillerJacket.Contracts.Messaging;

public interface IMessage
{
    string MessageType { get; }
    string TenantId { get; }
    string CorrelationId { get; }
    string? ExternalSource { get; }
    string? ExternalReferenceId { get; }
    string? RequestedByUserId { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract record MessageBase(
    string TenantId,
    string CorrelationId,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : IMessage
{
    public abstract string MessageType { get; }
}

public sealed record MessageEnvelope(
    string MessageType,
    string PayloadJson,
    string TenantId,
    string CorrelationId,
    DateTimeOffset EnqueuedAt
);
