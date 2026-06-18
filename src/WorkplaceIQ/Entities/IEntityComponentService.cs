namespace WorkplaceIQ.Entities;

public interface IEntityComponentService
{
    Task<EntityComponentResult> ResolveEntitiesAsync(
        EntityComponentRequest request,
        CancellationToken cancellationToken = default);

    Task<BusinessEntity> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<EntityRelationship> CreateRelationshipAsync(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
}
