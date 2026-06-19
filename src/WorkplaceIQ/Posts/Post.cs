using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Posts;

public sealed class Post
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContainerId { get; set; }

    public Guid? ContentId { get; set; }

    [ForeignKey(nameof(ContentId))]
    public Content.Content? Content { get; set; }

    [Required]
    [MaxLength(32)]
    public string PostType { get; set; } = PostTypes.Post;

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? AuthorUserId { get; set; }

    public bool IsSystemGenerated { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PostLabel> PostLabels { get; set; } = [];
}
