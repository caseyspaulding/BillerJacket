using System.ComponentModel.DataAnnotations;
using BillerJacket.Domain.Entities;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages;

[AllowAnonymous]
public class SetupModel : PageModel
{
    private readonly ArDbContext _db;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public SetupModel(ArDbContext db, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _db = db;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public SetupInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Redirect("/login");

        var identityUserId = _userManager.GetUserId(User);
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        if (user?.IsSetupComplete == true)
            return Redirect("/");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Redirect("/login");

        if (!ModelState.IsValid)
            return Page();

        var identityUserId = _userManager.GetUserId(User);
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        if (user is null)
        {
            ErrorMessage = "User record not found.";
            return Page();
        }

        if (user.IsSetupComplete)
            return Redirect("/");

        var tenant = new Tenant
        {
            TenantId = Guid.NewGuid(),
            Name = Input.CompanyName,
            DefaultCurrency = Input.DefaultCurrency
        };

        _db.Tenants.Add(tenant);
        user.TenantId = tenant.TenantId;
        user.IsSetupComplete = true;
        await _db.SaveChangesAsync();

        var appUser = await _userManager.FindByIdAsync(identityUserId!);
        await _signInManager.RefreshSignInAsync(appUser!);

        return Redirect("/");
    }

    public class SetupInput
    {
        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string DefaultCurrency { get; set; } = "USD";
    }
}
