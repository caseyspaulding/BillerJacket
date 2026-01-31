using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages.Payments;

public class IndexModel : PageModel
{
    private readonly ArDbContext _db;

    public IndexModel(ArDbContext db)
    {
        _db = db;
    }

    public List<PaymentRow> Payments { get; set; } = [];

    public async Task OnGetAsync()
    {
        Payments = await _db.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Customer)
            .OrderByDescending(p => p.AppliedAt)
            .Select(p => new PaymentRow
            {
                PaymentId = p.PaymentId,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                CustomerName = p.Invoice.Customer.DisplayName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                AppliedAt = p.AppliedAt,
                CorrelationId = p.CorrelationId
            })
            .ToListAsync();
    }

    public class PaymentRow
    {
        public Guid PaymentId { get; set; }
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTimeOffset AppliedAt { get; set; }
        public string? CorrelationId { get; set; }
    }
}
