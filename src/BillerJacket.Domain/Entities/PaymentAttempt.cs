using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class PaymentAttempt
{
    public Guid PaymentAttemptId { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public string? CorrelationId { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
