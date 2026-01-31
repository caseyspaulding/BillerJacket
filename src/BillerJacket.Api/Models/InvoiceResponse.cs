namespace BillerJacket.Api.Models;

public record InvoiceResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    string Status,
    Guid CustomerId,
    string? CustomerName,
    DateOnly IssueDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    DateTimeOffset? SentAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    List<InvoiceLineItemResponse> LineItems);

public record InvoiceLineItemResponse(
    Guid InvoiceLineItemId,
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
