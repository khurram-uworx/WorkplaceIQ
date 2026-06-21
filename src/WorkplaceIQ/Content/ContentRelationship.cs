using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkplaceIQ.Content;

public sealed class ContentRelationship
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SourceContentId { get; set; }

    [ForeignKey(nameof(SourceContentId))]
    public Content? SourceContent { get; set; }

    public Guid TargetContentId { get; set; }

    [ForeignKey(nameof(TargetContentId))]
    public Content? TargetContent { get; set; }

    [Required]
    [MaxLength(80)]
    public string RelationshipType { get; set; } = string.Empty;

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
