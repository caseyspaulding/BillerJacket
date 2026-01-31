using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Invoices;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public List<InvoiceRow> Invoices { get; set; } = [];

    public async Task OnGetAsync()
    {
        var query = _db.Invoices
            .Include(i => i.Customer)
            .AsQueryable();

        if (Enum.TryParse<InvoiceStatus>(Status, true, out var status))
        {
            query = query.Where(i => i.Status == status);
        }

        Invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvoiceRow
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                CustomerName = i.Customer.DisplayName,
                Status = i.Status,
                TotalAmount = i.TotalAmount,
                BalanceDue = i.TotalAmount - i.PaidAmount,
                DueDate = i.DueDate,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
    }

    public class InvoiceRow
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BalanceDue { get; set; }
        public DateOnly DueDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
