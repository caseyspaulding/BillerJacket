using System.ComponentModel.DataAnnotations;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Admin.Tenants;

public class EditModel : PageModel
{
    private readonly ArDbContext _db;

    public EditModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public TenantInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == id);

        if (tenant is null)
            return RedirectToPage("/Admin/Tenants/Index");

        Input = new TenantInput
        {
            TenantId = tenant.TenantId,
            Name = tenant.Name,
            DefaultCurrency = tenant.DefaultCurrency,
            IsActive = tenant.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == Input.TenantId);

        if (tenant is null)
        {
            ErrorMessage = "Tenant not found.";
            return Page();
        }

        tenant.Name = Input.Name;
        tenant.DefaultCurrency = Input.DefaultCurrency;
        tenant.IsActive = Input.IsActive;

        await _db.SaveChangesAsync();

        return RedirectToPage("/Admin/Tenants/Index");
    }

    public class TenantInput
    {
        public Guid TenantId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string DefaultCurrency { get; set; } = "USD";

        public bool IsActive { get; set; } = true;
    }
}
