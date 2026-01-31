using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class CommunicationLogConfiguration : IEntityTypeConfiguration<CommunicationLog>
{
    public void Configure(EntityTypeBuilder<CommunicationLog> builder)
    {
        builder.HasKey(e => e.CommunicationLogId);
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ToAddress).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500);
        builder.Property(e => e.Provider).HasMaxLength(100);
        builder.Property(e => e.ProviderMessageId).HasMaxLength(200);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.HasIndex(e => new { e.TenantId, e.InvoiceId });
        builder.HasOne(e => e.Customer).WithMany(c => c.CommunicationLogs).HasForeignKey(e => e.CustomerId);
        builder.HasOne(e => e.Invoice).WithMany(i => i.CommunicationLogs).HasForeignKey(e => e.InvoiceId);
    }
}
