using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class LandingPage
{
    public Guid LandingPageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public PageType PageType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
