using Dapper;
using Microsoft.Data.SqlClient;

namespace BillerJacket.Infrastructure.Reporting;

public class CustomerAgingQueries
{
    private readonly string _connectionString;

    public CustomerAgingQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<AgingRow>> GetAgingAsync(Guid tenantId)
    {
        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<AgingRow>("""
            SELECT
                c.CustomerId,
                c.DisplayName AS CustomerName,
                ISNULL(SUM(i.TotalAmount - i.PaidAmount), 0) AS TotalOutstanding,
                ISNULL(SUM(CASE WHEN DATEDIFF(DAY, i.DueDate, GETUTCDATE()) <= 0 THEN i.TotalAmount - i.PaidAmount ELSE 0 END), 0) AS CurrentAmount,
                ISNULL(SUM(CASE WHEN DATEDIFF(DAY, i.DueDate, GETUTCDATE()) BETWEEN 1 AND 30 THEN i.TotalAmount - i.PaidAmount ELSE 0 END), 0) AS Days1To30,
                ISNULL(SUM(CASE WHEN DATEDIFF(DAY, i.DueDate, GETUTCDATE()) BETWEEN 31 AND 60 THEN i.TotalAmount - i.PaidAmount ELSE 0 END), 0) AS Days31To60,
                ISNULL(SUM(CASE WHEN DATEDIFF(DAY, i.DueDate, GETUTCDATE()) BETWEEN 61 AND 90 THEN i.TotalAmount - i.PaidAmount ELSE 0 END), 0) AS Days61To90,
                ISNULL(SUM(CASE WHEN DATEDIFF(DAY, i.DueDate, GETUTCDATE()) > 90 THEN i.TotalAmount - i.PaidAmount ELSE 0 END), 0) AS Days90Plus
            FROM Customers c
            LEFT JOIN Invoices i ON i.CustomerId = c.CustomerId
                AND i.TenantId = @TenantId
                AND i.Status IN ('Sent', 'Overdue')
            WHERE c.TenantId = @TenantId
            GROUP BY c.CustomerId, c.DisplayName
            HAVING ISNULL(SUM(i.TotalAmount - i.PaidAmount), 0) > 0
            ORDER BY TotalOutstanding DESC
            """, new { TenantId = tenantId });
        return rows.AsList();
    }
}

public record AgingRow(
    Guid CustomerId,
    string CustomerName,
    decimal TotalOutstanding,
    decimal CurrentAmount,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus
);
