namespace WorkplaceIQ.Feeds;

public interface IFeedComponentService
{
    Task<FeedComponentResult> ResolveFeedAsync(
        FeedComponentRequest request,
        CancellationToken cancellationToken = default);
}
