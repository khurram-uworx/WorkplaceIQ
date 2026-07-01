using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;

namespace WorkplaceIQ.Tests.TestDoubles;

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

    public Task<ContentItem?> ResolveDetailAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ContentItem?>(null);
    }

    public Task<ContentItem> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ContentRelationship> CreateRelationshipAsync(
        Guid sourceContentId,
        Guid targetContentId,
        string relationshipType,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
