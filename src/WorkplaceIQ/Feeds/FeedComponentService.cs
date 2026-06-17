using WorkplaceIQ.Containers;

namespace WorkplaceIQ.Feeds;

public sealed class FeedComponentService(IWorkplaceIqStore store) : IFeedComponentService
{
    public async Task<FeedComponentResult> ResolveFeedAsync(
        FeedComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new ArgumentException("A feed id is required.", nameof(request));
        }

        var container = await store.GetContainerByKeyAsync(
            request.Id,
            ContainerTypes.Feed,
            cancellationToken);

        var created = false;

        if (container is null && request.AutoProvision)
        {
            container = await store.CreateContainerAsync(
                request.Id,
                ContainerTypes.Feed,
                request.Title ?? request.Id,
                cancellationToken);
            created = true;
        }

        if (container is null)
        {
            return new FeedComponentResult(
                null,
                [],
                false,
                true,
                request.Title ?? request.Id);
        }

        var posts = await store.GetFeedPostsAsync(container.Id, cancellationToken);

        return new FeedComponentResult(
            container,
            posts,
            created,
            false,
            container.Title);
    }
}
