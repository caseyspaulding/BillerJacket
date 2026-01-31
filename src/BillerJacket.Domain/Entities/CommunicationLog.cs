using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class CommunicationLog
{
    public Guid CommunicationLogId { get; set; }
    public Guid TenantId { get; set; }
    public CommunicationChannel Channel { get; set; }
    public CommunicationType Type { get; set; }
    public CommunicationStatus Status { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public string? CorrelationId { get; set; }

    public Customer? Customer { get; set; }
    public Invoice? Invoice { get; set; }
}
