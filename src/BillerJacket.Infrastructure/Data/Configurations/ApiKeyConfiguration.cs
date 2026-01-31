using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKeyRecord>
{
    public void Configure(EntityTypeBuilder<ApiKeyRecord> builder)
    {
        builder.HasKey(e => e.ApiKeyId);
        builder.Property(e => e.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.KeyHash).IsUnique();
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
