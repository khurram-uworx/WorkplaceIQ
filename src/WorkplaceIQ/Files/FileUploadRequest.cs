namespace WorkplaceIQ.Files;

public sealed record FileUploadRequest(
    string FilesId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    string? Title = null,
    string? Description = null,
    string? Labels = null,
    string? AuthorUserId = null);
