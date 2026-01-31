using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(e => e.TenantId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.DefaultCurrency).HasMaxLength(3).HasDefaultValue("USD");
    }
}
