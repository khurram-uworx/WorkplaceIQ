using System.ComponentModel.DataAnnotations;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Posts;

public sealed class Post
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

    public ICollection<PostLabel> PostLabels { get; set; } = [];
}
