namespace BillerJacket.Domain.Entities;

public class InvoiceLineItem
{
    public Guid InvoiceLineItemId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid TenantId { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public Invoice Invoice { get; set; } = null!;
}
