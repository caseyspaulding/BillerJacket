using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.IdentityUserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("Admin");
        builder.HasIndex(e => e.IdentityUserId).IsUnique();
        builder.HasOne(e => e.Tenant).WithMany(t => t.Users).HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
