using System.ComponentModel.DataAnnotations;

namespace WorkplaceIQ.Content;

public sealed class ContentRelationship
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SourceContentId { get; set; }
    public Content? SourceContent { get; set; }

    public Guid TargetContentId { get; set; }
    public Content? TargetContent { get; set; }

    [Required]
    [MaxLength(80)]
    public string RelationshipType { get; set; } = string.Empty;

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
