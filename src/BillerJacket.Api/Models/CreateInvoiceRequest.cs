using System.ComponentModel.DataAnnotations;

namespace BillerJacket.Api.Models;

public record CreateInvoiceRequest(
    [Required] Guid CustomerId,
    [Required] DateOnly IssueDate,
    [Required] DateOnly DueDate,
    string? CurrencyCode,
    decimal TaxAmount,
    [Required, MinLength(1)] List<CreateInvoiceLineItem> LineItems);

public record CreateInvoiceLineItem(
    [Required, MinLength(1)] string Description,
    [Range(0.01, double.MaxValue)] decimal Quantity,
    [Range(0.01, double.MaxValue)] decimal UnitPrice);
