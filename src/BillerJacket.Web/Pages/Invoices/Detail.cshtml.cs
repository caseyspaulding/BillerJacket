using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Invoices;

public class DetailModel : PageModel
{
    private readonly ArDbContext _db;

    public DetailModel(ArDbContext db)
    {
        _db = db;
    }

    public Invoice Invoice { get; set; } = null!;
    public List<Payment> Payments { get; set; } = [];
    public List<TimelineEntry> Timeline { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.LineItems.OrderBy(li => li.LineNumber))
            .Include(i => i.DunningState)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice is null)
            return NotFound();

        Invoice = invoice;

        Payments = await _db.Payments
            .Where(p => p.InvoiceId == id)
            .OrderByDescending(p => p.AppliedAt)
            .ToListAsync();

        var auditEntries = await _db.AuditLogs
            .Where(a => a.EntityType == "Invoice" && a.EntityId == id.ToString())
            .Include(a => a.PerformedByUser)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync();

        var commEntries = await _db.CommunicationLogs
            .Where(c => c.InvoiceId == id)
            .OrderByDescending(c => c.SentAt)
            .ToListAsync();

        Timeline = auditEntries.Select(a => new TimelineEntry
            {
                OccurredAt = a.OccurredAt,
                Action = a.Action,
                Detail = a.PerformedByUser?.Email ?? "System",
                CorrelationId = a.CorrelationId,
                Source = "Audit"
            })
            .Concat(commEntries.Select(c => new TimelineEntry
            {
                OccurredAt = c.SentAt,
                Action = $"{c.Type} {c.Channel} — {c.Status}",
                Detail = c.ToAddress,
                CorrelationId = c.CorrelationId,
                Source = "Communication"
            }))
            .OrderByDescending(t => t.OccurredAt)
            .ToList();

        return Page();
    }

    public class TimelineEntry
    {
        public DateTimeOffset OccurredAt { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
