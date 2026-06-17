using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Components;

public interface IComponentService
{
    Task<ComponentResult> ResolveAsync(
        ComponentRequest request,
        CancellationToken cancellationToken = default);

    Task<Post> CreatePostAsync(
        string componentId,
        string containerType,
        string componentName,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default);
}
