using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

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

    Task<IReadOnlyList<Post>> GetPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);

    Task<Post> CreatePostAsync(
        Guid containerId,
        string title,
        string body,
        IReadOnlyList<LabelName> labels,
        CancellationToken cancellationToken = default);
}
