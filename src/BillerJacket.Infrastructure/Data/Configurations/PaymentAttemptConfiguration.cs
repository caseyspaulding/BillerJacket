using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.HasKey(e => e.PaymentAttemptId);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrencyCode).HasMaxLength(3);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.FailureMessage).HasMaxLength(1000);
        builder.Property(e => e.Provider).HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.HasOne(e => e.Invoice).WithMany(i => i.PaymentAttempts).HasForeignKey(e => e.InvoiceId);
    }
}
