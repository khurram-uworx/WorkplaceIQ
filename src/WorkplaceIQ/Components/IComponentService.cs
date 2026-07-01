using WorkplaceIQ.Content;

namespace WorkplaceIQ.Components;

public interface IComponentService
{
    Task<ComponentResult> ResolveAsync(ComponentRequest request, CancellationToken cancellationToken = default);
    Task<ContentItem> CreatePostAsync(string componentId, string containerType, string componentName, string title, string body, string? labels = null, string? discriminator = null, CancellationToken cancellationToken = default);
}
