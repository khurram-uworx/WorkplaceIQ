using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

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

        var container = await store.GetContainerByKeyAsync(
            componentId,
            request.ContainerType,
            cancellationToken);

        var created = false;

        if (container is null && request.AutoProvision)
        {
            container = await store.CreateContainerAsync(
                componentId,
                request.ContainerType,
                initialTitle,
                cancellationToken);
            created = true;
        }

        if (container is null)
        {
            return new ComponentResult(
                null,
                [],
                false,
                true,
                initialTitle);
        }

        var posts = await store.GetPostsAsync(container.Id, cancellationToken);

        return new ComponentResult(
            container,
            posts,
            created,
            false,
            container.Title);
    }

    public async Task<Post> CreatePostAsync(
        string componentId,
        string containerType,
        string componentName,
        string title,
        string body,
        string? labels = null,
        string? postType = null,
        bool isSystemGenerated = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedComponentId = RequireValue(componentId, $"A {componentName} id is required.", nameof(componentId));
        var normalizedTitle = RequireValue(title, $"A {componentName} post title is required.", nameof(title));
        var normalizedBody = RequireValue(body, $"A {componentName} post body is required.", nameof(body));

        var container = await store.GetContainerByKeyAsync(
            normalizedComponentId,
            containerType,
            cancellationToken);

        if (container is null)
        {
            throw new InvalidOperationException($"{ToDisplayName(componentName)} '{normalizedComponentId}' does not exist.");
        }

        return await store.CreatePostAsync(
            container.Id,
            normalizedTitle,
            normalizedBody,
            LabelName.ParseList(labels),
            postType: postType,
            isSystemGenerated: isSystemGenerated,
            cancellationToken: cancellationToken);
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }

    private static string ToDisplayName(string value)
    {
        return value.Length == 0
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
