using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
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
                "EntityContainer",
                request.AutoProvision,
                "entity list"),
            cancellationToken);

        var container = result.Container as GroupContent;
        var entities = container is null
            ? []
            : await store.GetItemsByContainerAsync(container.Id, "member", cancellationToken);

        return new EntityComponentResult(
            container,
            entities,
            result.Created,
            result.Missing,
            result.DisplayTitle,
            entityType);
    }

    public async Task<ContentItem?> ResolveDetailAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var container = await store.GetContainerByNameAsync<GroupContent>(name, cancellationToken);
        if (container is not null)
        {
            var items = await store.GetItemsByContainerAsync(container.Id, cancellationToken: cancellationToken);
            return items.FirstOrDefault();
        }

        return await store.GetItemByIdAsync(Guid.TryParse(name, out var id) ? id : Guid.Empty, cancellationToken);
    }

    public async Task<ContentItem> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var listId = RequireValue(request.EntityListId, "An entity list id is required.", nameof(request));
        var entityType = RequireValue(request.EntityType, "An entity type is required.", nameof(request));
        var name = RequireValue(request.Name, "An entity name is required.", nameof(request));
        var title = RequireValue(request.Title, "An entity title is required.", nameof(request));

        var container = await store.GetContainerByNameAsync<GroupContent>(listId, cancellationToken)
            ?? throw new InvalidOperationException($"Entity list '{listId}' does not exist.");

        var now = DateTime.UtcNow;
        var entity = new ContentItem
        {
            ContainerId = container.Id,
            Discriminator = "member",
            Name = name,
            Title = title,
            Body = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim(),
            ContentData = string.IsNullOrWhiteSpace(request.MetadataJson) ? null : request.MetadataJson,
            CreatedAt = now,
            ModifiedAt = now
        };

        var created = await store.CreateItemAsync(entity, cancellationToken);

        foreach (var label in LabelName.ParseList(request.Labels))
        {
            await store.AddLabelToItemAsync(created.Id, label, cancellationToken);
        }

        return created;
    }

    public async Task<ContentRelationship> CreateRelationshipAsync(
        Guid sourceContentId,
        Guid targetContentId,
        string relationshipType,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceContentId == targetContentId)
            throw new ArgumentException("A relationship requires two different entities.", nameof(targetContentId));

        var normalizedType = RequireValue(relationshipType, "A relationship type is required.", nameof(relationshipType));

        var source = await store.GetItemByIdAsync(sourceContentId, cancellationToken);
        var target = await store.GetItemByIdAsync(targetContentId, cancellationToken);

        if (source is null || target is null)
            throw new InvalidOperationException("Both entities must exist before a relationship can be created.");

        // Relationships are at the container level — resolve container IDs
        return await store.CreateContentRelationshipAsync(
            new ContentRelationship
            {
                SourceContentId = source.ContainerId,
                TargetContentId = target.ContainerId,
                RelationshipType = normalizedType,
                MetadataJson = metadataJson
            },
            cancellationToken);
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, parameterName);
        return value.Trim();
    }
}
