using BillerJacket.Application.Common;
using BillerJacket.Domain.Enums;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Reporting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Web.Pages;

public class IndexModel : PageModel
{
    private readonly InvoiceDashboardQueries _dashboard;
    private readonly ArDbContext _db;

    public IndexModel(InvoiceDashboardQueries dashboard, ArDbContext db)
    {
        _dashboard = dashboard;
        _db = db;
    }

    public DashboardSummary Summary { get; set; } = new(0, 0, 0, 0);
    public List<RecentInvoice> RecentInvoices { get; set; } = [];
    public List<RecentPayment> RecentPayments { get; set; } = [];

    public async Task OnGetAsync()
    {
        var tenantId = Current.TenantIdOrNull;
        if (tenantId is null) return;

        Summary = await _dashboard.GetSummaryAsync(tenantId.Value);

        RecentInvoices = await _db.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .Select(i => new RecentInvoice
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

        RecentPayments = await _db.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Customer)
            .OrderByDescending(p => p.AppliedAt)
            .Take(10)
            .Select(p => new RecentPayment
            {
                PaymentId = p.PaymentId,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                CustomerName = p.Invoice.Customer.DisplayName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                AppliedAt = p.AppliedAt
            })
            .ToListAsync();
    }

    public class RecentInvoice
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

    public class RecentPayment
    {
        public Guid PaymentId { get; set; }
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTimeOffset AppliedAt { get; set; }
    }
}
