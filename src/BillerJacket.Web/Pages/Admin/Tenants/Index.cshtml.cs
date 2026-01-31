using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin.Tenants;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    public List<TenantRow> Tenants { get; set; } = [];

    public async Task OnGetAsync()
    {
        Tenants = await _db.Tenants
            .IgnoreQueryFilters()
            .OrderBy(t => t.Name)
            .Select(t => new TenantRow
            {
                TenantId = t.TenantId,
                Name = t.Name,
                DefaultCurrency = t.DefaultCurrency,
                IsActive = t.IsActive,
                UserCount = t.Users.Count,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public class TenantRow
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DefaultCurrency { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
