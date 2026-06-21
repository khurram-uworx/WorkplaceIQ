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
                ContentTypes.FileContainer,
                request.AutoProvision,
                "files"),
            cancellationToken);

        var files = result.Container is null
            ? []
            : await store.GetFilesByContainerAsync(result.Container.Id, cancellationToken);

        return new FileComponentResult(
            result.Container,
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
        {
            throw new ArgumentException("A non-empty file is required.", nameof(request.SizeBytes));
        }

        var container = await store.GetContentByNameAsync(filesId, cancellationToken)
            ?? throw new InvalidOperationException($"Files library '{filesId}' does not exist.");

        await storage.EnsureBucketAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var content = new Content.Content
        {
            ParentId = container.Id,
            ContentType = FileContentTypes.File,
            Name = CreateContentName(fileName),
            Title = string.IsNullOrWhiteSpace(request.Title) ? fileName : request.Title.Trim(),
            Body = request.Description?.Trim(),
            AuthorUserId = request.AuthorUserId?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = now,
            SearchText = fileName
        };

        var createdContent = await store.CreateContentAsync(content, cancellationToken);
        foreach (var label in LabelName.ParseList(request.Labels))
        {
            await store.AddLabelToContentAsync(createdContent.Id, label, cancellationToken);
        }

        try
        {
            var objectKey = CreateObjectKey(filesId, createdContent.Id, fileName);
            var stored = await storage.UploadAsync(
                objectKey,
                request.Content,
                NormalizeContentType(request.ContentType),
                request.SizeBytes,
                cancellationToken);

            var fileRecord = new FileRecord
            {
                ContentId = createdContent.Id,
                FileName = fileName,
                ContentType = NormalizeContentType(request.ContentType),
                SizeBytes = request.SizeBytes,
                ChecksumSha256 = stored.ChecksumSha256,
                StorageProvider = stored.StorageProvider,
                BucketName = stored.BucketName,
                ObjectKey = stored.ObjectKey,
                CreatedAt = now,
                UpdatedAt = now
            };

            return await store.CreateFileRecordAsync(fileRecord, cancellationToken);
        }
        catch
        {
            await store.DeleteContentAsync(createdContent.Id, cancellationToken);
            throw;
        }
    }

    public Task<FileObject?> GetFileAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        return store.GetFileByContentIdAsync(contentId, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        var file = await store.GetFileByContentIdAsync(contentId, cancellationToken)
            ?? throw new InvalidOperationException($"File content '{contentId}' not found.");

        return await storage.OpenReadAsync(file.FileRecord, cancellationToken);
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();
    }

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
