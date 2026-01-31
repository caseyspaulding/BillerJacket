namespace BillerJacket.Domain.Entities;

public class Customer
{
    public Guid CustomerId { get; set; }
    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ExternalSource { get; set; }
    public string? ExternalReferenceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<CommunicationLog> CommunicationLogs { get; set; } = [];
}
