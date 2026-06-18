using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Content;

public sealed class ContentItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContainerId { get; set; }

    [ForeignKey(nameof(ContainerId))]
    public Container? Container { get; set; }

    [Required]
    [MaxLength(128)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "published";

    [MaxLength(128)]
    public string? AuthorUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAt { get; set; }

    public string? MetadataJson { get; set; }

    public string? SearchText { get; set; }

    public ICollection<ContentLabel> ContentLabels { get; set; } = [];

    public ICollection<Post> Posts { get; set; } = [];
}
