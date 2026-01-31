using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(e => e.InvoiceId);
        builder.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CurrencyCode).HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(e => e.SubtotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ExternalSource).HasMaxLength(100);
        builder.Property(e => e.ExternalReferenceId).HasMaxLength(200);
        builder.Ignore(e => e.BalanceDue);
        builder.HasIndex(e => new { e.TenantId, e.InvoiceNumber }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.DueDate });
        builder.HasOne(e => e.Tenant).WithMany(t => t.Invoices).HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Customer).WithMany(c => c.Invoices).HasForeignKey(e => e.CustomerId);
    }
}
