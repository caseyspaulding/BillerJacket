using System.ComponentModel.DataAnnotations;
using BillerJacket.Domain.Entities;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillerJacket.Web.Pages;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ArDbContext _db;

    public RegisterModel(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ArDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var appUser = new AppUser { UserName = Input.Email, Email = Input.Email };
        var createResult = await _userManager.CreateAsync(appUser, Input.Password);

        if (!createResult.Succeeded)
        {
            ErrorMessage = string.Join(" ", createResult.Errors.Select(e => e.Description));
            return Page();
        }

        try
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                IdentityUserId = appUser.Id,
                Email = Input.Email,
                Role = "Admin",
                IsSetupComplete = false
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        catch
        {
            await _userManager.DeleteAsync(appUser);
            ErrorMessage = "Account creation failed. Please try again.";
            return Page();
        }

        await _signInManager.SignInAsync(appUser, isPersistent: false);
        return Redirect("/setup");
    }

    public class RegisterInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
