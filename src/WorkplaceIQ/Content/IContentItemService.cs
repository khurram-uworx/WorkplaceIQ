namespace WorkplaceIQ.Content;

public interface IContentItemService
{
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentItem>> GetByContainerAsync(Guid containerId, string? discriminator = null, CancellationToken cancellationToken = default);
    Task<ContentItem> CreateAsync(ContentItem item, CancellationToken cancellationToken = default);
    Task<ContentItem> UpdateAsync(ContentItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
