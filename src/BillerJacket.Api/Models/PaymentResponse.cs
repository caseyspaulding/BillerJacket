namespace BillerJacket.Api.Models;

public record PaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string Status,
    DateTimeOffset AppliedAt,
    string? ExternalProvider,
    string? ExternalPaymentId,
    string? CorrelationId,
    DateTimeOffset CreatedAt);
