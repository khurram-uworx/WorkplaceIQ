using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ;

public interface IWorkplaceIqStore
{
    // Label queries

    Task<Label?> GetLabelByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<Label> CreateLabelAsync(
        Label label,
        CancellationToken cancellationToken = default);

    // Classification queries

    Task<ClassifiedItem?> GetClassifiedItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClassifiedItem?> GetClassifiedByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassifiedItem>> GetClassifiedItemsByLabelAsync(
        Guid labelId,
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassifiedItem>> GetRecentClassifiedItemsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update a classification for a content item.
    /// Invariant: one ClassifiedItem per ContentId. If a classification already exists for this content,
    /// it is updated in-place (last classification wins). This is the sole entry point for persisting
    /// classification results — callers must not bypass this by manipulating ClassifiedItems directly.
    /// ADR 02: when Content is refactored into Container/ContentItem, this invariant must be preserved.
    /// </summary>
    Task<ClassifiedItem> UpsertClassifiedItemAsync(
        ClassifiedItem item,
        CancellationToken cancellationToken = default);

    Task<ClassifiedItem> UpdateClassifiedItemAsync(
        ClassifiedItem item,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetSignalCountsAsync(
        CancellationToken cancellationToken = default);

    Task DeleteClassifiedItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Content.Content> GetUnclassifiedContentsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    // Existing queries below
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
