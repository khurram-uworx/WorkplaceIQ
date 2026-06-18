namespace WorkplaceIQ.Files;

public interface IFileComponentService
{
    Task<FileComponentResult> ResolveFilesAsync(
        FileComponentRequest request,
        CancellationToken cancellationToken = default);

    Task<FileObject> UploadAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<FileObject?> GetFileAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);
}
