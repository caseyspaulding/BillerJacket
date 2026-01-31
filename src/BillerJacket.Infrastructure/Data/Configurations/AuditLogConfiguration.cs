using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.AuditLogId);
        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId });
        builder.HasIndex(e => new { e.TenantId, e.CorrelationId });
        builder.HasOne(e => e.PerformedByUser).WithMany().HasForeignKey(e => e.PerformedByUserId);
    }
}
