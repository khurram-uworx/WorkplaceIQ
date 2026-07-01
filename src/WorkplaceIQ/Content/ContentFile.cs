namespace WorkplaceIQ.Content;

public sealed class ContentFile
{
    public Guid Id { get; set; }
    public ContentItem? ContentItem { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
}
