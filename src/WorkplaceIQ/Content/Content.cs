using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Content;

public abstract class Content
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }

    public ICollection<ContentLabel> ContentLabels { get; set; } = [];
    public ICollection<ContentRelationship> SourceRelationships { get; set; } = [];
    public ICollection<ContentRelationship> TargetRelationships { get; set; } = [];
}
