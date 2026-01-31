using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.HasKey(e => e.InvoiceLineItemId);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Ignore(e => e.LineTotal);
        builder.HasOne(e => e.Invoice).WithMany(i => i.LineItems).HasForeignKey(e => e.InvoiceId);
    }
}
