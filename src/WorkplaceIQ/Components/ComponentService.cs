using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Components;

public sealed class ComponentService(IWorkplaceIqStore store) : IComponentService
{
    public async Task<ComponentResult> ResolveAsync(
        ComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var componentId = RequireValue(request.Id, $"A {request.ComponentName} id is required.", nameof(request));
        var initialTitle = string.IsNullOrWhiteSpace(request.Title)
            ? componentId
            : request.Title.Trim();

        var (container, created) = await ResolveContainerAsync(componentId, request, cancellationToken);
        if (container is null)
        {
            return new ComponentResult(null, [], false, true, initialTitle);
        }

        var items = await store.GetItemsByContainerAsync(container.Id, cancellationToken: cancellationToken);
        return new ComponentResult(container, items, created, false, container.Title);
    }

    public async Task<ContentItem> CreatePostAsync(
        string componentId,
        string containerType,
        string componentName,
        string title,
        string body,
        string? labels = null,
        string? discriminator = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedComponentId = RequireValue(componentId, $"A {componentName} id is required.", nameof(componentId));
        var normalizedTitle = RequireValue(title, $"A {componentName} post title is required.", nameof(title));
        var normalizedBody = RequireValue(body, $"A {componentName} post body is required.", nameof(body));

        var container = await LookupContainerAsync(normalizedComponentId, containerType, cancellationToken)
            ?? throw new InvalidOperationException($"{ToDisplayName(componentName)} '{normalizedComponentId}' does not exist.");

        var now = DateTime.UtcNow;
        var item = new ContentItem
        {
            ContainerId = container.Id,
            Discriminator = discriminator ?? "feed_entry",
            Name = normalizedTitle,
            Title = normalizedTitle,
            Body = normalizedBody,
            CreatedAt = now,
            ModifiedAt = now,
            PublishedAt = now
        };

        var created = await store.CreateItemAsync(item, cancellationToken);

        foreach (var label in LabelName.ParseList(labels))
        {
            await store.AddLabelToItemAsync(created.Id, label, cancellationToken);
        }

        return created;
    }

    private async Task<(Container? Container, bool Created)> ResolveContainerAsync(
        string componentId, ComponentRequest request, CancellationToken cancellationToken)
    {
        var container = await LookupContainerAsync(componentId, request.ContainerType, cancellationToken);
        if (container is not null) return (container, false);

        if (!request.AutoProvision) return (null, false);

        var created = request.ContainerType switch
        {
            "FeedContainer" => (Container)new FeedContent
            {
                Name = componentId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? componentId : request.Title.Trim(),
                RendererKey = "feed",
                IsSystemGenerated = false
            },
            "ForumContainer" => new DiscussionContent
            {
                Name = componentId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? componentId : request.Title.Trim(),
                RendererKey = "forum",
                IsSystemGenerated = false
            },
            "FileContainer" => new FolderContent
            {
                Name = componentId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? componentId : request.Title.Trim(),
                RendererKey = "files",
                IsSystemGenerated = false
            },
            "EntityContainer" => new GroupContent
            {
                Name = componentId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? componentId : request.Title.Trim(),
                RendererKey = "entities",
                IsSystemGenerated = false
            },
            _ => throw new InvalidOperationException($"Unknown container type: {request.ContainerType}")
        };

        return (await store.CreateContainerAsync(created, cancellationToken), true);
    }

    private Task<Container?> LookupContainerAsync(string name, string containerType, CancellationToken cancellationToken)
    {
        return containerType switch
        {
            "FeedContainer" => store.GetContainerByNameAsync<FeedContent>(name, cancellationToken).ContinueWith(t => (Container?)t.Result, cancellationToken),
            "ForumContainer" => store.GetContainerByNameAsync<DiscussionContent>(name, cancellationToken).ContinueWith(t => (Container?)t.Result, cancellationToken),
            "FileContainer" => store.GetContainerByNameAsync<FolderContent>(name, cancellationToken).ContinueWith(t => (Container?)t.Result, cancellationToken),
            "EntityContainer" => store.GetContainerByNameAsync<GroupContent>(name, cancellationToken).ContinueWith(t => (Container?)t.Result, cancellationToken),
            _ => Task.FromResult<Container?>(null)
        };
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, parameterName);
        return value.Trim();
    }

    private static string ToDisplayName(string value)
    {
        return value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
