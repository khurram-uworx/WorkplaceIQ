namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ.Feeds;
using WorkplaceIQ.Posts;

internal sealed class RecordingFeedComponentService(FeedComponentResult result) : IFeedComponentService
{
    public FeedComponentRequest? Request { get; private set; }

    public Task<FeedComponentResult> ResolveFeedAsync(
        FeedComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        return Task.FromResult(result);
    }

    public Task<Post> CreatePostAsync(
        string feedId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
