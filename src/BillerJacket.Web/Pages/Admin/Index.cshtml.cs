using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TotalBlogPosts { get; set; }
    public int PublishedBlogPosts { get; set; }
    public int TotalLandingPages { get; set; }
    public int PublishedLandingPages { get; set; }

    public async Task OnGetAsync()
    {
        TotalTenants = await _db.Tenants.CountAsync();
        ActiveTenants = await _db.Tenants.CountAsync(t => t.IsActive);

        TotalBlogPosts = await _db.BlogPosts.CountAsync();
        PublishedBlogPosts = await _db.BlogPosts.CountAsync(b => b.Status == ContentStatus.Published);

        TotalLandingPages = await _db.LandingPages.CountAsync();
        PublishedLandingPages = await _db.LandingPages.CountAsync(p => p.Status == ContentStatus.Published);
    }
}
