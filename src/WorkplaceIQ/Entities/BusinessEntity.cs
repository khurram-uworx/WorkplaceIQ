using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Entities;

public sealed class BusinessEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContainerId { get; set; }

    [ForeignKey(nameof(ContainerId))]
    public Container? Container { get; set; }

    [Required]
    [MaxLength(128)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = EntityStatuses.Active;

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<EntityLabel> EntityLabels { get; set; } = [];

    public ICollection<EntityRelationship> SourceRelationships { get; set; } = [];

    public ICollection<EntityRelationship> TargetRelationships { get; set; } = [];

    public ICollection<EntityContentLink> ContentLinks { get; set; } = [];

    public ICollection<EntityPostLink> PostLinks { get; set; } = [];

    public ICollection<EntityFileLink> FileLinks { get; set; } = [];
}
