using System.ComponentModel.DataAnnotations;

namespace WorkplaceIQ.Entities;

public sealed class EntityRelationship
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SourceEntityId { get; set; }

    public BusinessEntity? SourceEntity { get; set; }

    public Guid TargetEntityId { get; set; }

    public BusinessEntity? TargetEntity { get; set; }

    [Required]
    [MaxLength(80)]
    public string RelationshipType { get; set; } = string.Empty;

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
