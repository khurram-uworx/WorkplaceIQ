using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Content;

public sealed class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContainerId { get; set; }
    public Container? Container { get; set; }
    public string Discriminator { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? AuthorUserId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? PublishedAt { get; set; }
    public string? ContentData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public ICollection<ContentItemLabel> Labels { get; set; } = [];
}
