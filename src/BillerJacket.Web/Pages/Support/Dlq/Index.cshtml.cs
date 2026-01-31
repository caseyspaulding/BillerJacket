using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Support.Dlq;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    public List<DlqRow> FailedItems { get; set; } = [];

    public async Task OnGetAsync()
    {
        var failedWebhooks = await _db.WebhookEvents
            .Where(w => w.ProcessingStatus == WebhookProcessingStatus.Failed)
            .OrderByDescending(w => w.ReceivedAt)
            .Take(100)
            .Select(w => new DlqRow
            {
                Source = "Webhook",
                EntityId = w.WebhookEventId.ToString(),
                ErrorMessage = w.ErrorMessage,
                OccurredAt = w.ReceivedAt,
                CorrelationId = w.CorrelationId
            })
            .ToListAsync();

        var failedComms = await _db.CommunicationLogs
            .Where(c => c.Status == CommunicationStatus.Failed)
            .OrderByDescending(c => c.SentAt)
            .Take(100)
            .Select(c => new DlqRow
            {
                Source = "Communication",
                EntityId = c.CommunicationLogId.ToString(),
                ErrorMessage = c.ErrorMessage,
                OccurredAt = c.SentAt,
                CorrelationId = c.CorrelationId
            })
            .ToListAsync();

        FailedItems = failedWebhooks
            .Concat(failedComms)
            .OrderByDescending(i => i.OccurredAt)
            .Take(200)
            .ToList();
    }

    public class DlqRow
    {
        public string Source { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public string? CorrelationId { get; set; }
    }
}
