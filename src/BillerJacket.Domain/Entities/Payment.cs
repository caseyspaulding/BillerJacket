using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class Payment
{
    public Guid PaymentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalPaymentId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Invoice Invoice { get; set; } = null!;
    public User? CreatedByUser { get; set; }
}
