namespace BillerJacket.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string IdentityUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public bool IsSetupComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
