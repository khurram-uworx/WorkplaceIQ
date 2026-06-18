namespace WorkplaceIQ.Content;

public interface IContentService
{
    Task<IReadOnlyList<ContentItem>> GetByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);

    Task<ContentItem?> GetByIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<ContentItem> CreateAsync(
        Guid containerId,
        string contentType,
        string name,
        string title,
        string? body = null,
        string? authorUserId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<ContentItem> UpdateAsync(
        Guid contentItemId,
        string? title = null,
        string? body = null,
        string? status = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
}
