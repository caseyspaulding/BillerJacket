using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BillerJacket.Infrastructure.Identity;

public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLERJACKET_CONNECTION")
            ?? "Server=localhost,1433;Database=BillerJacket;User Id=SA;Password=YourStr0ng!Pass;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppIdentityDbContext(options);
    }
}
