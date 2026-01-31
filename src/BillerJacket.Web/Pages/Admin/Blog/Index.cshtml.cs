using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin.Blog;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public List<BlogPostRow> Posts { get; set; } = [];

    public async Task OnGetAsync()
    {
        var query = _db.BlogPosts.AsQueryable();

        if (Enum.TryParse<ContentStatus>(Status, true, out var status))
        {
            query = query.Where(b => b.Status == status);
        }

        Posts = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BlogPostRow
            {
                BlogPostId = b.BlogPostId,
                Title = b.Title,
                Slug = b.Slug,
                Status = b.Status,
                AuthorEmail = b.Author != null ? b.Author.Email : null,
                PublishedAt = b.PublishedAt,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();
    }

    public class BlogPostRow
    {
        public Guid BlogPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public ContentStatus Status { get; set; }
        public string? AuthorEmail { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
