using System.ComponentModel.DataAnnotations;

namespace WorkplaceIQ.Feeds;

public sealed class FeedPost
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContainerId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
