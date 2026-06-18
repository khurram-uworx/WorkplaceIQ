using WorkplaceIQ.Content;
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

        var content = await store.GetContentByNameAsync(
            componentId,
            cancellationToken);

        var created = false;

        if (content is null && request.AutoProvision)
        {
            content = new Content.Content
            {
                Name = componentId,
                ContentType = request.ContainerType,
                Title = initialTitle,
                RendererKey = request.ContainerType,
                IsSystemGenerated = false
            };
            content = await store.CreateContentAsync(content, cancellationToken);
            created = true;
        }

        if (content is null)
        {
            return new ComponentResult(
                null,
                [],
                false,
                true,
                initialTitle);
        }

        var posts = await store.GetPostsAsync(content.Id, cancellationToken);

        return new ComponentResult(
            content,
            posts,
            created,
            false,
            content.Title);
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

        var content = await store.GetContentByNameAsync(
            normalizedComponentId,
            cancellationToken);

        if (content is null)
        {
            throw new InvalidOperationException($"{ToDisplayName(componentName)} '{normalizedComponentId}' does not exist.");
        }

        return await store.CreatePostAsync(
            content.Id,
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
