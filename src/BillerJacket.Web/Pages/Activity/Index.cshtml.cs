using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Activity;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    public List<ActivityRow> Activities { get; set; } = [];

    public async Task OnGetAsync()
    {
        var query = _db.AuditLogs
            .Include(a => a.PerformedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(EntityType))
        {
            query = query.Where(a => a.EntityType == EntityType);
        }

        Activities = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(200)
            .Select(a => new ActivityRow
            {
                AuditLogId = a.AuditLogId,
                OccurredAt = a.OccurredAt,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                PerformedBy = a.PerformedByUser != null ? a.PerformedByUser.Email : "System",
                CorrelationId = a.CorrelationId
            })
            .ToListAsync();
    }

    public class ActivityRow
    {
        public Guid AuditLogId { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
    }
}
