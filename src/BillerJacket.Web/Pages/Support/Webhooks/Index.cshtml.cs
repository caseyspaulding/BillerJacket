using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Support.Webhooks;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public List<WebhookRow> Webhooks { get; set; } = [];

    public async Task OnGetAsync()
    {
        var query = _db.WebhookEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Status) && Enum.TryParse<WebhookProcessingStatus>(Status, out var status))
        {
            query = query.Where(w => w.ProcessingStatus == status);
        }

        Webhooks = await query
            .OrderByDescending(w => w.ReceivedAt)
            .Take(200)
            .Select(w => new WebhookRow
            {
                WebhookEventId = w.WebhookEventId,
                Provider = w.Provider,
                EventType = w.EventType,
                ProcessingStatus = w.ProcessingStatus.ToString(),
                ReceivedAt = w.ReceivedAt,
                ProcessedAt = w.ProcessedAt,
                ErrorMessage = w.ErrorMessage,
                CorrelationId = w.CorrelationId
            })
            .ToListAsync();
    }

    public class WebhookRow
    {
        public Guid WebhookEventId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ProcessingStatus { get; set; } = string.Empty;
        public DateTimeOffset ReceivedAt { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CorrelationId { get; set; }
    }
}
