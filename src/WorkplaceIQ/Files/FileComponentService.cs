using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Files;

public sealed class FileComponentService(
    IComponentService componentService,
    IWorkplaceIqStore store,
    IFileObjectStorage storage) : IFileComponentService
{
    public async Task<FileComponentResult> ResolveFilesAsync(
        FileComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await componentService.ResolveAsync(
            new ComponentRequest(
                request.Id,
                request.Title ?? string.Empty,
                "FileContainer",
                request.AutoProvision,
                "files"),
            cancellationToken);

        var container = result.Container as FolderContent;
        var files = container is null
            ? []
            : await GetFilesForContainerAsync(container.Id, cancellationToken);

        return new FileComponentResult(
            container,
            files,
            result.Created,
            result.Missing,
            result.DisplayTitle);
    }

    public async Task<FileObject> UploadAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var filesId = RequireValue(request.FilesId, "A files id is required.", nameof(request.FilesId));
        var fileName = Path.GetFileName(RequireValue(request.FileName, "A file name is required.", nameof(request.FileName)));
        if (request.SizeBytes <= 0)
            throw new ArgumentException("A non-empty file is required.", nameof(request.SizeBytes));

        var container = await store.GetContainerByNameAsync<FolderContent>(filesId, cancellationToken)
            ?? throw new InvalidOperationException($"Files library '{filesId}' does not exist.");

        await storage.EnsureBucketAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var item = new ContentItem
        {
            ContainerId = container.Id,
            Discriminator = "file",
            Name = CreateContentName(fileName),
            Title = string.IsNullOrWhiteSpace(request.Title) ? fileName : request.Title.Trim(),
            Body = request.Description?.Trim(),
            AuthorUserId = request.AuthorUserId?.Trim(),
            CreatedAt = now,
            ModifiedAt = now,
            PublishedAt = now
        };

        var createdItem = await store.CreateItemAsync(item, cancellationToken);

        foreach (var label in LabelName.ParseList(request.Labels))
        {
            await store.AddLabelToItemAsync(createdItem.Id, label, cancellationToken);
        }

        try
        {
            var objectKey = CreateObjectKey(filesId, createdItem.Id, fileName);
            var stored = await storage.UploadAsync(
                objectKey,
                request.Content,
                NormalizeContentType(request.ContentType),
                request.SizeBytes,
                cancellationToken);

            var contentFile = new ContentFile
            {
                Id = createdItem.Id,
                FileName = fileName,
                ContentType = NormalizeContentType(request.ContentType),
                SizeBytes = request.SizeBytes,
                ChecksumSha256 = stored.ChecksumSha256,
                StorageProvider = stored.StorageProvider,
                BucketName = stored.BucketName,
                ObjectKey = stored.ObjectKey
            };

            await store.CreateContentFileAsync(contentFile, cancellationToken);
            return new FileObject(createdItem, contentFile);
        }
        catch
        {
            await store.DeleteItemAsync(createdItem.Id, cancellationToken);
            throw;
        }
    }

    public async Task<FileObject?> GetFileAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item = await store.GetItemByIdAsync(itemId, cancellationToken);
        if (item is null) return null;

        var file = await store.GetContentFileByItemIdAsync(itemId, cancellationToken);
        if (file is null) return null;

        return new FileObject(item, file);
    }

    public async Task<Stream> OpenReadAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var file = await store.GetContentFileByItemIdAsync(itemId, cancellationToken)
            ?? throw new InvalidOperationException($"File content '{itemId}' not found.");

        return await storage.OpenReadAsync(file, cancellationToken);
    }

    private async Task<IReadOnlyList<FileObject>> GetFilesForContainerAsync(
        Guid containerId, CancellationToken cancellationToken)
    {
        var items = await store.GetItemsByContainerAsync(containerId, "file", cancellationToken);
        var results = new List<FileObject>(items.Count);

        foreach (var item in items)
        {
            var cf = await store.GetContentFileByItemIdAsync(item.Id, cancellationToken);
            if (cf is not null)
                results.Add(new FileObject(item, cf));
        }

        return results.OrderByDescending(f => f.ContentItem.ModifiedAt).ToList();
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, parameterName);
        return value.Trim();
    }

    private static string NormalizeContentType(string? contentType)
        => string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();

    private static string CreateContentName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(name)
            ? Guid.NewGuid().ToString("N")
            : name.Trim().Replace(' ', '-').ToLowerInvariant();
    }

    private static string CreateObjectKey(string filesId, Guid contentId, string fileName)
    {
        var safeFileName = Uri.EscapeDataString(fileName);
        return $"containers/{filesId}/content/{contentId:N}/original/{safeFileName}";
    }
}
