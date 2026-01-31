namespace BillerJacket.Domain.Entities;

public class InvoiceDunningState
{
    public Guid InvoiceId { get; set; }
    public Guid TenantId { get; set; }
    public Guid DunningPlanId { get; set; }
    public int CurrentStepNumber { get; set; }
    public DateTimeOffset? NextActionAt { get; set; }
    public DateTimeOffset? LastActionAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public DunningPlan DunningPlan { get; set; } = null!;
}
