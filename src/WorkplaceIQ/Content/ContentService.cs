namespace WorkplaceIQ.Content;

public sealed class ContentService(IWorkplaceIqStore store) : IContentService
{
    public Task<IReadOnlyList<ContentItem>> GetByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return store.GetContentByContainerAsync(containerId, cancellationToken);
    }

    public Task<ContentItem?> GetByIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        return store.GetContentByIdAsync(contentItemId, cancellationToken);
    }

    public async Task<ContentItem> CreateAsync(
        Guid containerId,
        string contentType,
        string name,
        string title,
        string? body = null,
        string? authorUserId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var item = new ContentItem
        {
            ContainerId = containerId,
            ContentType = contentType.Trim(),
            Name = name.Trim(),
            Title = title.Trim(),
            Body = body?.Trim(),
            AuthorUserId = authorUserId?.Trim(),
            MetadataJson = metadataJson,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = now
        };

        return await store.CreateContentAsync(item, cancellationToken);
    }

    public async Task<ContentItem> UpdateAsync(
        Guid contentItemId,
        string? title = null,
        string? body = null,
        string? status = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var item = await store.GetContentByIdAsync(contentItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Content item '{contentItemId}' not found.");

        if (title is not null) item.Title = title.Trim();
        if (body is not null) item.Body = body.Trim();
        if (status is not null) item.Status = status.Trim();
        if (metadataJson is not null) item.MetadataJson = metadataJson;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        return await store.UpdateContentAsync(item, cancellationToken);
    }
}
