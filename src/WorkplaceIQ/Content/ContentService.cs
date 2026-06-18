namespace WorkplaceIQ.Content;

public sealed class ContentService(IWorkplaceIqStore store) : IContentService
{
    public Task<IReadOnlyList<Content>> GetByParentAsync(
        Guid parentId,
        CancellationToken cancellationToken = default)
    {
        return store.GetChildrenAsync(parentId, cancellationToken: cancellationToken);
    }

    public Task<Content?> GetByIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        return store.GetContentByIdAsync(contentId, cancellationToken);
    }

    public async Task<Content> CreateAsync(
        Guid parentId,
        string contentType,
        string name,
        string title,
        string? body = null,
        string? authorUserId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var item = new Content
        {
            ParentId = parentId,
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

    public async Task<Content> UpdateAsync(
        Guid contentId,
        string? title = null,
        string? body = null,
        string? status = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var item = await store.GetContentByIdAsync(contentId, cancellationToken)
            ?? throw new InvalidOperationException($"Content item '{contentId}' not found.");

        if (title is not null) item.Title = title.Trim();
        if (body is not null) item.Body = body.Trim();
        if (status is not null) item.Status = status.Trim();
        if (metadataJson is not null) item.MetadataJson = metadataJson;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        return await store.UpdateContentAsync(item, cancellationToken);
    }
}
