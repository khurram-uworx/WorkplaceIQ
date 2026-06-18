namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ.Files;

internal sealed class InMemoryFileObjectStorage : IFileObjectStorage
{
    private readonly Dictionary<string, byte[]> objects = [];

    public string ProviderName => "Memory";

    public string BucketName => "test-files";

    public bool BucketEnsured { get; private set; }

    public IReadOnlyDictionary<string, byte[]> Objects => objects;

    public Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        BucketEnsured = true;
        return Task.CompletedTask;
    }

    public async Task<StoredFileObject> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        objects[objectKey] = memory.ToArray();

        return new StoredFileObject(
            ProviderName,
            BucketName,
            objectKey,
            ChecksumSha256: null);
    }

    public Task<Stream> OpenReadAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(new MemoryStream(objects[fileRecord.ObjectKey]));
    }

    public Task DeleteAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        objects.Remove(fileRecord.ObjectKey);
        return Task.CompletedTask;
    }
}
