using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(e => e.CustomerId);
        builder.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.ExternalSource).HasMaxLength(100);
        builder.Property(e => e.ExternalReferenceId).HasMaxLength(200);
        builder.HasIndex(e => new { e.TenantId, e.Email });
        builder.HasOne(e => e.Tenant).WithMany(t => t.Customers).HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
