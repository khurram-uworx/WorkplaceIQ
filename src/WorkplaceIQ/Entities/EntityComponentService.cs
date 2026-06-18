using WorkplaceIQ.Components;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Entities;

public sealed class EntityComponentService(
    IComponentService componentService,
    IWorkplaceIqStore store) : IEntityComponentService
{
    public async Task<EntityComponentResult> ResolveEntitiesAsync(
        EntityComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var entityType = string.IsNullOrWhiteSpace(request.Type)
            ? "Entity"
            : request.Type.Trim();

        var result = await componentService.ResolveAsync(
            new ComponentRequest(
                request.Id,
                request.Title ?? string.Empty,
                ContainerTypes.EntityList,
                request.AutoProvision,
                "entity list"),
            cancellationToken);

        var entities = result.Container is null
            ? []
            : await store.GetEntitiesByContainerAsync(result.Container.Id, cancellationToken);

        return new EntityComponentResult(
            result.Container,
            entities,
            result.Created,
            result.Missing,
            result.DisplayTitle,
            entityType);
    }

    public async Task<BusinessEntity> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var listId = RequireValue(request.EntityListId, "An entity list id is required.", nameof(request));
        var entityType = RequireValue(request.EntityType, "An entity type is required.", nameof(request));
        var name = RequireValue(request.Name, "An entity name is required.", nameof(request));
        var title = RequireValue(request.Title, "An entity title is required.", nameof(request));

        var container = await store.GetContainerByKeyAsync(
            listId,
            ContainerTypes.EntityList,
            cancellationToken);

        if (container is null)
        {
            throw new InvalidOperationException($"Entity list '{listId}' does not exist.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new BusinessEntity
        {
            ContainerId = container.Id,
            EntityType = entityType,
            Name = name,
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? EntityStatuses.Active : request.Status.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? null : request.MetadataJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        return await store.CreateEntityAsync(
            entity,
            LabelName.ParseList(request.Labels),
            cancellationToken);
    }

    public async Task<EntityRelationship> CreateRelationshipAsync(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceEntityId == targetEntityId)
        {
            throw new ArgumentException("An entity relationship requires two different entities.", nameof(targetEntityId));
        }

        var normalizedType = RequireValue(relationshipType, "An entity relationship type is required.", nameof(relationshipType));

        var source = await store.GetEntityByIdAsync(sourceEntityId, cancellationToken);
        var target = await store.GetEntityByIdAsync(targetEntityId, cancellationToken);

        if (source is null || target is null)
        {
            throw new InvalidOperationException("Both entities must exist before a relationship can be created.");
        }

        return await store.CreateEntityRelationshipAsync(
            new EntityRelationship
            {
                SourceEntityId = sourceEntityId,
                TargetEntityId = targetEntityId,
                RelationshipType = normalizedType,
                MetadataJson = metadataJson
            },
            cancellationToken);
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }
}
