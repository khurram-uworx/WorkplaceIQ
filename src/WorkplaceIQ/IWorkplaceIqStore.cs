using WorkplaceIQ.Containers;
using WorkplaceIQ.Feeds;

namespace WorkplaceIQ;

public interface IWorkplaceIqStore
{
    Task<Container?> GetContainerByKeyAsync(
        string key,
        string type,
        CancellationToken cancellationToken = default);

    Task<Container> CreateContainerAsync(
        string key,
        string type,
        string title,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedPost>> GetFeedPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);
}
