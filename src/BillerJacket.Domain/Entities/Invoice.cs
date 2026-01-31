using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class Invoice
{
    public Guid InvoiceId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue => TotalAmount - PaidAmount;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? ExternalSource { get; set; }
    public string? ExternalReferenceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = [];
    public ICollection<CommunicationLog> CommunicationLogs { get; set; } = [];
    public InvoiceDunningState? DunningState { get; set; }
}
