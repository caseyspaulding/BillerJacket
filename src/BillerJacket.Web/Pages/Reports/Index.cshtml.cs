using BillerJacket.Application.Common;
using BillerJacket.Infrastructure.Reporting;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillerJacket.Web.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly CustomerAgingQueries _aging;

    public IndexModel(CustomerAgingQueries aging)
    {
        _aging = aging;
    }

    public IReadOnlyList<AgingRow> Rows { get; set; } = [];

    public decimal TotalCurrent { get; set; }
    public decimal Total1To30 { get; set; }
    public decimal Total31To60 { get; set; }
    public decimal Total61To90 { get; set; }
    public decimal Total90Plus { get; set; }
    public decimal GrandTotal { get; set; }

    public async Task OnGetAsync()
    {
        var tenantId = Current.TenantIdOrNull;
        if (tenantId is null) return;

        Rows = await _aging.GetAgingAsync(tenantId.Value);

        TotalCurrent = Rows.Sum(r => r.CurrentAmount);
        Total1To30 = Rows.Sum(r => r.Days1To30);
        Total31To60 = Rows.Sum(r => r.Days31To60);
        Total61To90 = Rows.Sum(r => r.Days61To90);
        Total90Plus = Rows.Sum(r => r.Days90Plus);
        GrandTotal = Rows.Sum(r => r.TotalOutstanding);
    }
}
