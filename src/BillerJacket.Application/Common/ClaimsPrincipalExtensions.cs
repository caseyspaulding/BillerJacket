using System.Security.Claims;

namespace BillerJacket.Application.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("user_id"), out var id) ? id : null;

    public static Guid? GetTenantId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("tenant_id"), out var id) ? id : null;

    public static string? GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue("role");

    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue("email");
}
