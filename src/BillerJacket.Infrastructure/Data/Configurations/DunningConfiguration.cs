using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class DunningPlanConfiguration : IEntityTypeConfiguration<DunningPlan>
{
    public void Configure(EntityTypeBuilder<DunningPlan> builder)
    {
        builder.HasKey(e => e.DunningPlanId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(e => e.Tenant).WithMany(t => t.DunningPlans).HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DunningStepConfiguration : IEntityTypeConfiguration<DunningStep>
{
    public void Configure(EntityTypeBuilder<DunningStep> builder)
    {
        builder.HasKey(e => e.DunningStepId);
        builder.Property(e => e.TemplateKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => new { e.DunningPlanId, e.StepNumber }).IsUnique();
        builder.HasOne(e => e.DunningPlan).WithMany(p => p.Steps).HasForeignKey(e => e.DunningPlanId);
    }
}

public class InvoiceDunningStateConfiguration : IEntityTypeConfiguration<InvoiceDunningState>
{
    public void Configure(EntityTypeBuilder<InvoiceDunningState> builder)
    {
        builder.HasKey(e => e.InvoiceId);
        builder.HasOne(e => e.Invoice).WithOne(i => i.DunningState).HasForeignKey<InvoiceDunningState>(e => e.InvoiceId);
        builder.HasOne(e => e.DunningPlan).WithMany().HasForeignKey(e => e.DunningPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
