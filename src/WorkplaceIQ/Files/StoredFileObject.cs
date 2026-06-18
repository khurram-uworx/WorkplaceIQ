namespace WorkplaceIQ.Files;

public sealed record StoredFileObject(
    string StorageProvider,
    string BucketName,
    string ObjectKey,
    string? ChecksumSha256);
