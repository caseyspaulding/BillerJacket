using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.HasKey(e => e.WebhookEventId);
        builder.Property(e => e.Provider).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ExternalEventId).HasMaxLength(200);
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProcessingStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalEventId }).IsUnique()
            .HasFilter("[ExternalEventId] IS NOT NULL");
    }
}
