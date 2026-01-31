using System.ComponentModel.DataAnnotations;
using BillerJacket.Application.Common;
using BillerJacket.Domain.Entities;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Customers;

public class EditModel : PageModel
{
    private readonly ArDbContext _db;

    public EditModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public CustomerInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool IsNew => Input.CustomerId == Guid.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id.Value);

            if (customer is null)
                return RedirectToPage("/Customers/Index");

            Input = new CustomerInput
            {
                CustomerId = customer.CustomerId,
                DisplayName = customer.DisplayName,
                Email = customer.Email,
                Phone = customer.Phone,
                IsActive = customer.IsActive
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.CustomerId == Guid.Empty)
        {
            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                TenantId = Current.TenantId,
                DisplayName = Input.DisplayName,
                Email = Input.Email,
                Phone = Input.Phone,
                IsActive = Input.IsActive,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Customers.Add(customer);
        }
        else
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == Input.CustomerId);

            if (customer is null)
            {
                ErrorMessage = "Customer not found.";
                return Page();
            }

            customer.DisplayName = Input.DisplayName;
            customer.Email = Input.Email;
            customer.Phone = Input.Phone;
            customer.IsActive = Input.IsActive;
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Customers/Index");
    }

    public class CustomerInput
    {
        public Guid CustomerId { get; set; }

        [Required, MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
