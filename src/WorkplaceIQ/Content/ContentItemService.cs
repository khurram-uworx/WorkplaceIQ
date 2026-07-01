namespace WorkplaceIQ.Content;

public sealed class ContentItemService(IWorkplaceIqStore store) : IContentItemService
{
    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetItemByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<ContentItem>> GetByContainerAsync(Guid containerId, string? discriminator = null, CancellationToken cancellationToken = default)
        => store.GetItemsByContainerAsync(containerId, discriminator, cancellationToken);

    public Task<ContentItem> CreateAsync(ContentItem item, CancellationToken cancellationToken = default)
        => store.CreateItemAsync(item, cancellationToken);

    public Task<ContentItem> UpdateAsync(ContentItem item, CancellationToken cancellationToken = default)
        => store.UpdateItemAsync(item, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => store.DeleteItemAsync(id, cancellationToken);
}
