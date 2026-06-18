using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ;

public interface IWorkplaceIqStore
{
    Task<Content.Content?> GetContentByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<Content.Content?> GetContentByIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Content.Content>> GetChildrenAsync(
        Guid parentId,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Content.Content>> GetContentByTypeAsync(
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Content.Content> CreateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default);

    Task<Content.Content> UpdateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default);

    Task DeleteContentAsync(
        Guid contentId,
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

    Task<IReadOnlyList<FileObject>> GetFilesByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);

    Task<FileObject?> GetFileByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);

    Task<FileObject> CreateFileRecordAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default);

    Task<ContentRelationship> CreateContentRelationshipAsync(
        ContentRelationship relationship,
        CancellationToken cancellationToken = default);

    Task AddLabelToContentAsync(
        Guid contentId,
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
