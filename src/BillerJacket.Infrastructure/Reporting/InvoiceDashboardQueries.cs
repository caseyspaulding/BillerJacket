using Dapper;
using Microsoft.Data.SqlClient;

namespace BillerJacket.Infrastructure.Reporting;

public class InvoiceDashboardQueries
{
    private readonly string _connectionString;

    public InvoiceDashboardQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DashboardSummary> GetSummaryAsync(Guid tenantId)
    {
        await using var conn = new SqlConnection(_connectionString);
        return await conn.QuerySingleAsync<DashboardSummary>("""
            SELECT
                ISNULL(SUM(CASE WHEN Status IN ('Sent','Overdue') THEN TotalAmount - PaidAmount ELSE 0 END), 0) AS TotalOutstanding,
                ISNULL(SUM(CASE WHEN Status = 'Overdue' THEN TotalAmount - PaidAmount ELSE 0 END), 0) AS TotalOverdue,
                ISNULL(SUM(CASE WHEN Status = 'Paid' AND PaidAt >= DATEADD(DAY, -30, GETUTCDATE()) THEN PaidAmount ELSE 0 END), 0) AS PaidLast30Days,
                COUNT(CASE WHEN Status IN ('Sent','Overdue') THEN 1 END) AS OpenInvoiceCount
            FROM Invoices
            WHERE TenantId = @TenantId
            """, new { TenantId = tenantId });
    }
}

public record DashboardSummary(
    decimal TotalOutstanding,
    decimal TotalOverdue,
    decimal PaidLast30Days,
    int OpenInvoiceCount
);
