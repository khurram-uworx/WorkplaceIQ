using WorkplaceIQ.Content;

namespace WorkplaceIQ.Entities;

public sealed class EntityContentLink
{
    public Guid EntityId { get; set; }

    public BusinessEntity? Entity { get; set; }

    public Guid ContentItemId { get; set; }

    public ContentItem? ContentItem { get; set; }
}
