namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ.Forums;
using WorkplaceIQ.Posts;

internal sealed class RecordingForumComponentService(ForumComponentResult result) : IForumComponentService
{
    public ForumComponentRequest? Request { get; private set; }

    public Task<ForumComponentResult> ResolveForumAsync(
        ForumComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        return Task.FromResult(result);
    }

    public Task<Post> CreateThreadAsync(
        string forumId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
