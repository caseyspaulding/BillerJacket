namespace BillerJacket.Domain.Entities;

public class DunningPlan
{
    public Guid DunningPlanId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<DunningStep> Steps { get; set; } = [];
}
