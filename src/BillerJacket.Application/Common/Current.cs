using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BillerJacket.Application.Common;

public static class Current
{
    private static IHttpContextAccessor _httpContextAccessor = null!;

    public static void Initialize(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private static ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public static Guid TenantId =>
        TenantIdOrNull ?? throw new InvalidOperationException("TenantId not available.");

    public static Guid? TenantIdOrNull =>
        Guid.TryParse(User?.FindFirstValue("tenant_id"), out var id) ? id : null;

    public static Guid UserId =>
        UserIdOrNull ?? throw new InvalidOperationException("UserId not available.");

    public static Guid? UserIdOrNull =>
        Guid.TryParse(User?.FindFirstValue("user_id"), out var id) ? id : null;

    public static string? Email => User?.FindFirstValue("email");

    public static string Role => User?.FindFirstValue("role") ?? string.Empty;

    public static bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public static bool IsSuperAdmin => Role == "SuperAdmin";

    public static bool IsSystemUser => UserIdOrNull == SystemUser.Id;

    public static string CorrelationId =>
        _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
