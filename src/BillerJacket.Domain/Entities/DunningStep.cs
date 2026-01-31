namespace BillerJacket.Domain.Entities;

public class DunningStep
{
    public Guid DunningStepId { get; set; }
    public Guid TenantId { get; set; }
    public Guid DunningPlanId { get; set; }
    public int StepNumber { get; set; }
    public int DaysAfterDue { get; set; }
    public string TemplateKey { get; set; } = string.Empty;

    public DunningPlan DunningPlan { get; set; } = null!;
}
