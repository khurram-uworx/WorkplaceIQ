namespace WorkplaceIQ.Files;

public interface IFileObjectStorage
{
    string ProviderName { get; }

    string BucketName { get; }

    Task EnsureBucketAsync(CancellationToken cancellationToken = default);

    Task<StoredFileObject> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default);
}
