using WorkplaceIQ.Content;

namespace WorkplaceIQ.Feeds;

public interface IFeedComponentService
{
    Task<FeedComponentResult> ResolveFeedAsync(
        FeedComponentRequest request,
        CancellationToken cancellationToken = default);

    Task<ContentItem> CreatePostAsync(
        string feedId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default);
}
