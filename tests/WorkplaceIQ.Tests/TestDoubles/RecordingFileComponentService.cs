namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ.Files;

internal sealed class RecordingFileComponentService(FileComponentResult result) : IFileComponentService
{
    public FileComponentRequest? Request { get; private set; }

    public Task<FileComponentResult> ResolveFilesAsync(
        FileComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        return Task.FromResult(result);
    }

    public Task<FileObject> UploadAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<FileObject?> GetFileAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<Stream> OpenReadAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
