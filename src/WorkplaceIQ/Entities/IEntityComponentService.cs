using WorkplaceIQ.Content;

namespace WorkplaceIQ.Entities;

public interface IEntityComponentService
{
    Task<EntityComponentResult> ResolveEntitiesAsync(
        EntityComponentRequest request,
        CancellationToken cancellationToken = default);

    Task<ContentItem?> ResolveDetailAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<ContentItem> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ContentRelationship> CreateRelationshipAsync(
        Guid sourceContentId,
        Guid targetContentId,
        string relationshipType,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
}
