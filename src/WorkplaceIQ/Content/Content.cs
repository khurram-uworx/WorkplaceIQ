using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Content;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(ContentType))]
[Index(nameof(ParentId))]
[Index(nameof(Status))]
public sealed class Content
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public Content? Parent { get; set; }

    public ICollection<Content> Children { get; set; } = [];

    [Required]
    [MaxLength(128)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? Description { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "active";

    [MaxLength(128)]
    public string? AuthorUserId { get; set; }

    [MaxLength(128)]
    public string? RendererKey { get; set; }

    public string? SettingsJson { get; set; }

    public bool IsSystemGenerated { get; set; }

    public string? MetadataJson { get; set; }

    public string? SearchText { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAt { get; set; }

    public ICollection<ContentLabel> ContentLabels { get; set; } = [];

    public ICollection<Post> Posts { get; set; } = [];

    public ICollection<ContentRelationship> SourceRelationships { get; set; } = [];

    public ICollection<ContentRelationship> TargetRelationships { get; set; } = [];
}
