using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.HasKey(e => e.IdempotencyKeyId);
        builder.Property(e => e.Operation).HasMaxLength(100).IsRequired();
        builder.Property(e => e.KeyValue).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RequestHash).HasMaxLength(64);
        builder.HasIndex(e => new { e.TenantId, e.Operation, e.KeyValue }).IsUnique();
    }
}
