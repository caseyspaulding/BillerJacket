using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin.Pages;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    public List<LandingPageRow> LandingPages { get; set; } = [];

    public async Task OnGetAsync()
    {
        var query = _db.LandingPages.AsQueryable();

        if (Enum.TryParse<ContentStatus>(Status, true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (Enum.TryParse<PageType>(Type, true, out var pageType))
        {
            query = query.Where(p => p.PageType == pageType);
        }

        LandingPages = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new LandingPageRow
            {
                LandingPageId = p.LandingPageId,
                Title = p.Title,
                Slug = p.Slug,
                PageType = p.PageType,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public class LandingPageRow
    {
        public Guid LandingPageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public PageType PageType { get; set; }
        public ContentStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
