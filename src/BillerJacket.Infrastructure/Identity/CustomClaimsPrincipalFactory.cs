using System.Security.Claims;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillerJacket.Infrastructure.Identity;

public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser>
{
    private readonly ArDbContext _db;

    public CustomClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        ArDbContext db)
        : base(userManager, optionsAccessor)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser appUser)
    {
        var identity = await base.GenerateClaimsAsync(appUser);

        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.IdentityUserId == appUser.Id);

        if (user is null)
        {
            identity.AddClaim(new Claim("setup_complete", "false"));
            return identity;
        }

        identity.AddClaim(new Claim("user_id", user.UserId.ToString()));
        identity.AddClaim(new Claim("role", user.Role));
        identity.AddClaim(new Claim("email", user.Email));
        identity.AddClaim(new Claim("setup_complete", user.IsSetupComplete.ToString().ToLowerInvariant()));

        if (user.TenantId.HasValue)
            identity.AddClaim(new Claim("tenant_id", user.TenantId.Value.ToString()));

        return identity;
    }
}
