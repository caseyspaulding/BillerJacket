namespace BillerJacket.Domain.Entities;

public class IdempotencyKey
{
    public Guid IdempotencyKeyId { get; set; }
    public Guid TenantId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string KeyValue { get; set; } = string.Empty;
    public string? RequestHash { get; set; }
    public string? ResponseJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
