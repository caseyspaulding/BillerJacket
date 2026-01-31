using System.ComponentModel.DataAnnotations;

namespace BillerJacket.Api.Models;

public record RecordPaymentRequest(
    [Required] Guid InvoiceId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    string? CurrencyCode,
    [Required] string PaymentMethod,
    string? ExternalProvider,
    string? ExternalPaymentId);
