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
                ContentTypes.EntityContainer,
                request.AutoProvision,
                "entity list"),
            cancellationToken);

        var entities = result.Container is null
            ? []
            : await store.GetChildrenAsync(result.Container.Id, cancellationToken: cancellationToken);

        return new EntityComponentResult(
            result.Container,
            entities,
            result.Created,
            result.Missing,
            result.DisplayTitle,
            entityType);
    }

    public Task<Content.Content?> ResolveDetailAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return store.GetContentByNameAsync(name, cancellationToken);
    }

    public async Task<Content.Content> CreateEntityAsync(
        EntityCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var listId = RequireValue(request.EntityListId, "An entity list id is required.", nameof(request));
        var entityType = RequireValue(request.EntityType, "An entity type is required.", nameof(request));
        var name = RequireValue(request.Name, "An entity name is required.", nameof(request));
        var title = RequireValue(request.Title, "An entity title is required.", nameof(request));

        var container = await store.GetContentByNameAsync(
            listId,
            cancellationToken);

        if (container is null)
        {
            throw new InvalidOperationException($"Entity list '{listId}' does not exist.");
        }

        var now = DateTime.UtcNow;
        var entity = new Content.Content
        {
            ParentId = container.Id,
            ContentType = entityType,
            Name = name,
            Title = title,
            Body = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? null : request.MetadataJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await store.CreateContentAsync(entity, cancellationToken);

        foreach (var label in LabelName.ParseList(request.Labels))
        {
            await store.AddLabelToContentAsync(created.Id, label, cancellationToken);
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
        {
            throw new ArgumentException("A relationship requires two different entities.", nameof(targetContentId));
        }

        var normalizedType = RequireValue(relationshipType, "A relationship type is required.", nameof(relationshipType));

        var source = await store.GetContentByIdAsync(sourceContentId, cancellationToken);
        var target = await store.GetContentByIdAsync(targetContentId, cancellationToken);

        if (source is null || target is null)
        {
            throw new InvalidOperationException("Both entities must exist before a relationship can be created.");
        }

        return await store.CreateContentRelationshipAsync(
            new ContentRelationship
            {
                SourceContentId = sourceContentId,
                TargetContentId = targetContentId,
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
