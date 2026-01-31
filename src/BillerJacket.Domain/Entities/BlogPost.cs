using BillerJacket.Domain.Enums;

namespace BillerJacket.Domain.Entities;

public class BlogPost
{
    public Guid BlogPostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public Guid? AuthorUserId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? Author { get; set; }
}
