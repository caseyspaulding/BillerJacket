namespace BillerJacket.Domain.Entities;

public class ApiKeyRecord
{
    public Guid ApiKeyId { get; set; }
    public Guid TenantId { get; set; }
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}
