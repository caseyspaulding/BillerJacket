namespace BillerJacket.Domain.Entities;

public class AuditLog
{
    public Guid AuditLogId { get; set; }
    public Guid TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }

    public User? PerformedByUser { get; set; }
}
