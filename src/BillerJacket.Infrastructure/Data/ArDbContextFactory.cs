using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BillerJacket.Infrastructure.Data;

public class ArDbContextFactory : IDesignTimeDbContextFactory<ArDbContext>
{
    public ArDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLERJACKET_CONNECTION")
            ?? "Server=localhost,1433;Database=BillerJacket;User Id=SA;Password=YourStr0ng!Pass;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ArDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ArDbContext(options);
    }
}
