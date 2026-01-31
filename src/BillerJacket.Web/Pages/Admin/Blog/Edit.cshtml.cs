using System.ComponentModel.DataAnnotations;
using BillerJacket.Application.Common;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin.Blog;

public class EditModel : PageModel
{
    private readonly ArDbContext _db;

    public EditModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public BlogPostInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool IsNew => Input.BlogPostId == Guid.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            var post = await _db.BlogPosts
                .FirstOrDefaultAsync(b => b.BlogPostId == id.Value);

            if (post is null)
                return RedirectToPage("/Admin/Blog/Index");

            Input = new BlogPostInput
            {
                BlogPostId = post.BlogPostId,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = post.Content,
                Status = post.Status
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.BlogPostId == Guid.Empty)
        {
            var post = new BlogPost
            {
                BlogPostId = Guid.NewGuid(),
                Title = Input.Title,
                Slug = Input.Slug,
                Excerpt = Input.Excerpt,
                Content = Input.Content,
                Status = Input.Status,
                AuthorUserId = Current.UserIdOrNull,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            if (post.Status == ContentStatus.Published)
                post.PublishedAt = DateTimeOffset.UtcNow;

            _db.BlogPosts.Add(post);
        }
        else
        {
            var post = await _db.BlogPosts
                .FirstOrDefaultAsync(b => b.BlogPostId == Input.BlogPostId);

            if (post is null)
            {
                ErrorMessage = "Blog post not found.";
                return Page();
            }

            var wasPublished = post.Status == ContentStatus.Published;

            post.Title = Input.Title;
            post.Slug = Input.Slug;
            post.Excerpt = Input.Excerpt;
            post.Content = Input.Content;
            post.Status = Input.Status;
            post.UpdatedAt = DateTimeOffset.UtcNow;

            if (!wasPublished && post.Status == ContentStatus.Published)
                post.PublishedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Admin/Blog/Index");
    }

    public class BlogPostInput
    {
        public Guid BlogPostId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Excerpt { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public ContentStatus Status { get; set; } = ContentStatus.Draft;
    }
}
