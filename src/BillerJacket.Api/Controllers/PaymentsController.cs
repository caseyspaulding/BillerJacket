using System.Text.Json;
using BillerJacket.Api.Models;
using BillerJacket.Application.Common;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ArDbContext _db;
    private readonly LoggingContext _logging;

    public PaymentsController(ArDbContext db, ILogger<PaymentsController> logger)
    {
        _db = db;
        _logging = new LoggingContext(logger);
    }

    [HttpPost]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var tenantId = Current.TenantId;

        using var _ = _logging.WithContext(
            feature: "Payment",
            operation: "RecordPayment",
            component: "API",
            tenantId: tenantId);

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader)
            || string.IsNullOrWhiteSpace(idempotencyHeader))
        {
            return BadRequest(new { error = "Idempotency-Key header is required." });
        }

        var keyValue = idempotencyHeader.ToString();

        var existingKey = await _db.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Operation == "payment" && k.KeyValue == keyValue, ct);

        if (existingKey is not null)
        {
            var cached = JsonSerializer.Deserialize<PaymentResponse>(existingKey.ResponseJson!);
            return Ok(cached);
        }

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId, ct);

        if (invoice is null)
            return BadRequest(new { error = "Invoice not found." });

        if (invoice.Status is not (InvoiceStatus.Sent or InvoiceStatus.Overdue))
            return BadRequest(new { error = $"Invoice must be Sent or Overdue to accept payment. Current status: {invoice.Status}." });

        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var method))
            return BadRequest(new { error = $"Invalid payment method: {request.PaymentMethod}. Valid values: Manual, Autopay, External." });

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoice.InvoiceId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode ?? invoice.CurrencyCode,
            Method = method,
            Status = PaymentStatus.Succeeded,
            AppliedAt = DateTimeOffset.UtcNow,
            ExternalProvider = request.ExternalProvider,
            ExternalPaymentId = request.ExternalPaymentId,
            CreatedByUserId = Current.UserIdOrNull,
            CorrelationId = Current.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Payments.Add(payment);

        invoice.PaidAmount += request.Amount;
        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTimeOffset.UtcNow;
        }

        var response = new PaymentResponse(
            payment.PaymentId,
            payment.InvoiceId,
            payment.Amount,
            payment.CurrencyCode,
            payment.Method.ToString(),
            payment.Status.ToString(),
            payment.AppliedAt,
            payment.ExternalProvider,
            payment.ExternalPaymentId,
            payment.CorrelationId,
            payment.CreatedAt);

        _db.IdempotencyKeys.Add(new IdempotencyKey
        {
            IdempotencyKeyId = Guid.NewGuid(),
            TenantId = tenantId,
            Operation = "payment",
            KeyValue = keyValue,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAt = DateTimeOffset.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = "Payment",
            EntityId = payment.PaymentId.ToString(),
            Action = "payment.recorded",
            DataJson = JsonSerializer.Serialize(new { payment.InvoiceId, payment.Amount, payment.Method }),
            PerformedByUserId = Current.UserIdOrNull,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = Current.CorrelationId
        });

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(RecordPayment), new { id = payment.PaymentId }, response);
    }
}
