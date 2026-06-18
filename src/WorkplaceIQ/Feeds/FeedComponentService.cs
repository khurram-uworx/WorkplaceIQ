using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Feeds;

public sealed class FeedComponentService(
    IComponentService componentService,
    IWorkplaceIqStore store) : IFeedComponentService
{
    public async Task<FeedComponentResult> ResolveFeedAsync(
        FeedComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await componentService.ResolveAsync(
            new ComponentRequest(
                request.Id,
                request.Title ?? string.Empty,
                ContentTypes.FeedContainer,
                request.AutoProvision,
                "feed"),
            cancellationToken);

        var contentItems = result.Container is null
            ? []
            : await store.GetChildrenAsync(result.Container.Id, cancellationToken: cancellationToken);

        return new FeedComponentResult(
            result.Container,
            result.Posts,
            contentItems,
            result.Created,
            result.Missing,
            result.DisplayTitle);
    }

    public Task<Post> CreatePostAsync(
        string feedId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default)
    {
        return componentService.CreatePostAsync(
            feedId,
            ContentTypes.FeedContainer,
            "feed",
            title,
            body,
            labels,
            cancellationToken: cancellationToken);
    }
}
