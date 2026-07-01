using WorkplaceIQ.Components;
using WorkplaceIQ.Content;

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
                "FeedContainer",
                request.AutoProvision,
                "feed"),
            cancellationToken);

        var container = result.Container as FeedContent;
        var items = container is null
            ? []
            : await store.GetItemsByContainerAsync(container.Id, cancellationToken: cancellationToken);

        return new FeedComponentResult(
            container,
            items,
            result.Created,
            result.Missing,
            result.DisplayTitle);
    }

    public Task<ContentItem> CreatePostAsync(
        string feedId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default)
    {
        return componentService.CreatePostAsync(
            feedId,
            "FeedContainer",
            "feed",
            title,
            body,
            labels,
            discriminator: "feed_entry",
            cancellationToken: cancellationToken);
    }
}
