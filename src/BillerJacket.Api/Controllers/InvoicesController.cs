using System.Text.Json;
using BillerJacket.Infrastructure.Messaging;
using BillerJacket.Api.Models;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly ArDbContext _db;
    private readonly IBusPublisher _bus;
    private readonly LoggingContext _logging;

    public InvoicesController(ArDbContext db, IBusPublisher bus, ILogger<InvoicesController> logger)
    {
        _db = db;
        _bus = bus;
        _logging = new LoggingContext(logger);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var tenantId = Current.TenantId;

        using var _ = _logging.WithContext(
            feature: "Invoice",
            operation: "Create",
            component: "API",
            tenantId: tenantId);

        if (request.LineItems is not { Count: > 0 })
            return BadRequest(new { error = "At least one line item is required." });

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, ct);

        if (customer is null)
            return BadRequest(new { error = "Customer not found." });

        var subtotal = request.LineItems.Sum(li => li.Quantity * li.UnitPrice);
        var total = subtotal + request.TaxAmount;

        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = request.CustomerId,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Draft,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            CurrencyCode = request.CurrencyCode ?? "USD",
            SubtotalAmount = subtotal,
            TaxAmount = request.TaxAmount,
            TotalAmount = total,
            PaidAmount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Invoices.Add(invoice);

        var lineNumber = 1;
        foreach (var li in request.LineItems)
        {
            _db.InvoiceLineItems.Add(new InvoiceLineItem
            {
                InvoiceLineItemId = Guid.NewGuid(),
                InvoiceId = invoice.InvoiceId,
                TenantId = tenantId,
                LineNumber = lineNumber++,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            });
        }

        _db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = "Invoice",
            EntityId = invoice.InvoiceId.ToString(),
            Action = "invoice.created",
            DataJson = JsonSerializer.Serialize(new { invoice.InvoiceNumber, invoice.CustomerId, invoice.TotalAmount }),
            PerformedByUserId = Current.UserIdOrNull,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = Current.CorrelationId
        });

        await _db.SaveChangesAsync(ct);

        var response = MapInvoiceResponse(invoice, customer.DisplayName);

        return CreatedAtAction(nameof(Create), new { id = invoice.InvoiceId }, response);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
    {
        var tenantId = Current.TenantId;

        using var _ = _logging.WithContext(
            feature: "Invoice",
            operation: "Send",
            component: "API",
            tenantId: tenantId);

        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.InvoiceId == id, ct);

        if (invoice is null)
            return NotFound(new { error = "Invoice not found." });

        if (invoice.Status != InvoiceStatus.Draft)
            return BadRequest(new { error = $"Invoice must be in Draft status to send. Current status: {invoice.Status}." });

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTimeOffset.UtcNow;

        await _bus.PublishAsync(Queues.EmailSend, new InvoiceEmailRequested(
            TenantId: tenantId.ToString(),
            CorrelationId: Current.CorrelationId,
            InvoiceId: invoice.InvoiceId.ToString(),
            ToEmail: invoice.Customer.Email,
            Subject: $"Invoice {invoice.InvoiceNumber}",
            Body: $"You have a new invoice ({invoice.InvoiceNumber}) for {invoice.TotalAmount:C}.",
            ExternalSource: null,
            ExternalReferenceId: null,
            RequestedByUserId: Current.UserIdOrNull?.ToString(),
            OccurredAt: DateTimeOffset.UtcNow), ct);

        _db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = "Invoice",
            EntityId = invoice.InvoiceId.ToString(),
            Action = "invoice.sent",
            DataJson = JsonSerializer.Serialize(new { invoice.InvoiceNumber, ToEmail = invoice.Customer.Email }),
            PerformedByUserId = Current.UserIdOrNull,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = Current.CorrelationId
        });

        await _db.SaveChangesAsync(ct);

        var response = MapInvoiceResponse(invoice, invoice.Customer.DisplayName);

        return Ok(response);
    }

    private static InvoiceResponse MapInvoiceResponse(Invoice invoice, string? customerName)
    {
        var lineItems = (invoice.LineItems ?? [])
            .OrderBy(li => li.LineNumber)
            .Select(li => new InvoiceLineItemResponse(
                li.InvoiceLineItemId,
                li.LineNumber,
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.LineTotal))
            .ToList();

        return new InvoiceResponse(
            invoice.InvoiceId,
            invoice.InvoiceNumber,
            invoice.Status.ToString(),
            invoice.CustomerId,
            customerName,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.CurrencyCode,
            invoice.SubtotalAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.BalanceDue,
            invoice.SentAt,
            invoice.PaidAt,
            invoice.CreatedAt,
            lineItems);
    }
}
