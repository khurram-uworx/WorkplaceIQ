using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ;

public interface IWorkplaceIqStore
{
    Task<Container?> GetContainerByKeyAsync(
        string key,
        string type,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Container>> GetContainersAsync(
        string? type = null,
        CancellationToken cancellationToken = default);

    Task<Container> CreateContainerAsync(
        string key,
        string type,
        string title,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Post>> GetPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);

    Task<Post?> GetPostByIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<Post> CreatePostAsync(
        Guid containerId,
        string title,
        string body,
        IReadOnlyList<LabelName> labels,
        Guid? contentId = null,
        string? postType = null,
        string? authorUserId = null,
        bool isSystemGenerated = false,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<Post> UpdatePostAsync(
        Post post,
        CancellationToken cancellationToken = default);

    Task DeletePostAsync(
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContentItem>> GetContentByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);

    Task<ContentItem?> GetContentByIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<ContentItem> CreateContentAsync(
        ContentItem item,
        CancellationToken cancellationToken = default);

    Task<ContentItem> UpdateContentAsync(
        ContentItem item,
        CancellationToken cancellationToken = default);

    Task DeleteContentAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task AddLabelToContentAsync(
        Guid contentItemId,
        LabelName label,
        CancellationToken cancellationToken = default);

    Task AddLabelToPostAsync(
        Guid postId,
        LabelName label,
        CancellationToken cancellationToken = default);

    Task<MetricDefinition?> GetMetricDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default);
}
