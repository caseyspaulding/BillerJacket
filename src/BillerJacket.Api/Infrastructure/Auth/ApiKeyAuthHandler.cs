using System.Security.Claims;
using System.Text.Encodings.Web;
using BillerJacket.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillerJacket.Api.Infrastructure.Auth;

public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ArDbContext _db;

    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ArDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
            return AuthenticateResult.Fail("Missing X-Api-Key header.");

        if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
            return AuthenticateResult.Fail("Missing X-Tenant-Id header.");

        var rawKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.Fail("Empty API key.");

        if (!Guid.TryParse(tenantIdHeader.ToString(), out var tenantId))
            return AuthenticateResult.Fail("Invalid X-Tenant-Id format.");

        var hash = ApiKeyHasher.Hash(rawKey);

        var record = await _db.ApiKeys
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.TenantId == tenantId && k.IsActive);

        if (record is null)
            return AuthenticateResult.Fail("Invalid API key or tenant.");

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("api_key_name", record.Name)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
