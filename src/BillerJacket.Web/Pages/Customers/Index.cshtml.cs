using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    public List<CustomerRow> Customers { get; set; } = [];

    public async Task OnGetAsync()
    {
        Customers = await _db.Customers
            .OrderBy(c => c.DisplayName)
            .Select(c => new CustomerRow
            {
                CustomerId = c.CustomerId,
                DisplayName = c.DisplayName,
                Email = c.Email,
                Phone = c.Phone,
                IsActive = c.IsActive,
                InvoiceCount = c.Invoices.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public class CustomerRow
    {
        public Guid CustomerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public int InvoiceCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
