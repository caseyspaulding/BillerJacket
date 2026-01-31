using BillerJacket.Domain.Entities;
using BillerJacket.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BillerJacket.Infrastructure.Data;

public static class SeedData
{
    private static readonly Guid DevTenantId = new("11111111-1111-1111-1111-111111111111");
    private const string SuperAdminEmail = "admin@billerjacket.com";
    private const string DefaultDevPassword = "Admin123!";

    public static async Task EnsureSeedDataAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        // Run pending migrations
        var arDb = sp.GetRequiredService<ArDbContext>();
        await arDb.Database.MigrateAsync();
        logger.LogInformation("ArDbContext migrations applied.");

        var identityDb = sp.GetRequiredService<AppIdentityDbContext>();
        await identityDb.Database.MigrateAsync();
        logger.LogInformation("AppIdentityDbContext migrations applied.");

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();

        // Create SuperAdmin identity user
        var existingAdmin = await userManager.FindByEmailAsync(SuperAdminEmail);
        if (existingAdmin is null)
        {
            var appUser = new AppUser { UserName = SuperAdminEmail, Email = SuperAdminEmail };
            var result = await userManager.CreateAsync(appUser, DefaultDevPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create SuperAdmin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
            existingAdmin = appUser;
            logger.LogInformation("SuperAdmin identity user created ({Email}).", SuperAdminEmail);
        }

        // Create dev tenant
        if (!await arDb.Tenants.IgnoreQueryFilters().AnyAsync(t => t.TenantId == DevTenantId))
        {
            arDb.Tenants.Add(new Tenant
            {
                TenantId = DevTenantId,
                Name = "Dev Tenant",
                DefaultCurrency = "USD"
            });
            await arDb.SaveChangesAsync();
            logger.LogInformation("Dev Tenant created ({Id}).", DevTenantId);
        }

        // Create SuperAdmin user row
        if (!await arDb.Users.IgnoreQueryFilters().AnyAsync(u => u.IdentityUserId == existingAdmin.Id))
        {
            arDb.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                IdentityUserId = existingAdmin.Id,
                Email = SuperAdminEmail,
                Role = "SuperAdmin",
                IsSetupComplete = true,
                TenantId = null
            });
            await arDb.SaveChangesAsync();
            logger.LogInformation("SuperAdmin User row created.");
        }

        // Create dev API key
        if (!await arDb.ApiKeys.IgnoreQueryFilters().AnyAsync(k => k.TenantId == DevTenantId))
        {
            var rawKey = $"bj_dev_{Guid.NewGuid():N}";
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

            arDb.ApiKeys.Add(new ApiKeyRecord
            {
                ApiKeyId = Guid.NewGuid(),
                TenantId = DevTenantId,
                KeyHash = hash,
                Name = "Dev API Key"
            });
            await arDb.SaveChangesAsync();

            logger.LogWarning("Dev API Key created. Raw key (save this): {RawKey}", rawKey);
            logger.LogWarning("Dev Tenant ID: {TenantId}", DevTenantId);
        }
    }
}
