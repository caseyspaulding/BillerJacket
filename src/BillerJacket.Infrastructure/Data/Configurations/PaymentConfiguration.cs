using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.PaymentId);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrencyCode).HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(e => e.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ExternalProvider).HasMaxLength(100);
        builder.Property(e => e.ExternalPaymentId).HasMaxLength(200);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.HasIndex(e => new { e.TenantId, e.InvoiceId });
        builder.HasOne(e => e.Invoice).WithMany(i => i.Payments).HasForeignKey(e => e.InvoiceId);
        builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId);
    }
}
