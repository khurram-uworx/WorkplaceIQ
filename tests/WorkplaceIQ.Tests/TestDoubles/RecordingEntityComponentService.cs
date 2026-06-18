namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ.Entities;

internal sealed class RecordingEntityComponentService(EntityComponentResult result) : IEntityComponentService
{
    public EntityComponentRequest? Request { get; private set; }

    public Task<EntityComponentResult> ResolveEntitiesAsync(
        EntityComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        return Task.FromResult(result);
    }

    public Task<BusinessEntity> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<EntityRelationship> CreateRelationshipAsync(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
