namespace WorkplaceIQ.Content;

public interface IContentService
{
    Task<IReadOnlyList<Content>> GetByParentAsync(
        Guid parentId,
        CancellationToken cancellationToken = default);

    Task<Content?> GetByIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);

    Task<Content> CreateAsync(
        Guid parentId,
        string contentType,
        string name,
        string title,
        string? body = null,
        string? authorUserId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<Content> UpdateAsync(
        Guid contentId,
        string? title = null,
        string? body = null,
        string? status = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
}
