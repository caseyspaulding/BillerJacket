namespace BillerJacket.Domain.Entities;

public class Tenant
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<DunningPlan> DunningPlans { get; set; } = [];
}
