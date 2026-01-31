using System.ComponentModel.DataAnnotations;
using BillerJacket.Domain.Entities;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin.Pages;

public class EditModel : PageModel
{
    private readonly ArDbContext _db;

    public EditModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public LandingPageInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool IsNew => Input.LandingPageId == Guid.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            var page = await _db.LandingPages
                .FirstOrDefaultAsync(p => p.LandingPageId == id.Value);

            if (page is null)
                return RedirectToPage("/Admin/Pages/Index");

            Input = new LandingPageInput
            {
                LandingPageId = page.LandingPageId,
                Title = page.Title,
                Slug = page.Slug,
                PageType = page.PageType,
                MetaDescription = page.MetaDescription,
                Content = page.Content,
                Status = page.Status
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.LandingPageId == Guid.Empty)
        {
            var page = new LandingPage
            {
                LandingPageId = Guid.NewGuid(),
                Title = Input.Title,
                Slug = Input.Slug,
                PageType = Input.PageType,
                MetaDescription = Input.MetaDescription,
                Content = Input.Content,
                Status = Input.Status,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.LandingPages.Add(page);
        }
        else
        {
            var page = await _db.LandingPages
                .FirstOrDefaultAsync(p => p.LandingPageId == Input.LandingPageId);

            if (page is null)
            {
                ErrorMessage = "Landing page not found.";
                return Page();
            }

            page.Title = Input.Title;
            page.Slug = Input.Slug;
            page.PageType = Input.PageType;
            page.MetaDescription = Input.MetaDescription;
            page.Content = Input.Content;
            page.Status = Input.Status;
            page.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Admin/Pages/Index");
    }

    public class LandingPageInput
    {
        public Guid LandingPageId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Slug { get; set; } = string.Empty;

        public PageType PageType { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public ContentStatus Status { get; set; } = ContentStatus.Draft;
    }
}
