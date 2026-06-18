using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Entities;

public sealed class EntityPostLink
{
    public Guid EntityId { get; set; }

    public BusinessEntity? Entity { get; set; }

    public Guid PostId { get; set; }

    public Post? Post { get; set; }
}
