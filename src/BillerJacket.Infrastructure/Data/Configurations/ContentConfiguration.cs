using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillerJacket.Infrastructure.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasKey(e => e.BlogPostId);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Excerpt).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorUserId);
    }
}

public class LandingPageConfiguration : IEntityTypeConfiguration<LandingPage>
{
    public void Configure(EntityTypeBuilder<LandingPage> builder)
    {
        builder.HasKey(e => e.LandingPageId);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(300).IsRequired();
        builder.Property(e => e.PageType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.MetaDescription).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.Slug).IsUnique();
    }
}
