using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ;

public interface IWorkplaceIqStore
{
    // Container queries
    Task<T?> GetContainerByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Container;
    Task<T?> GetContainerByNameAsync<T>(string name, CancellationToken cancellationToken = default) where T : Container;
    Task<IReadOnlyList<T>> GetContainersByTypeAsync<T>(CancellationToken cancellationToken = default) where T : Container;
    Task<IReadOnlyList<Container>> GetAllContainersAsync(CancellationToken cancellationToken = default);
    Task<T> CreateContainerAsync<T>(T container, CancellationToken cancellationToken = default) where T : Container;
    Task<T> UpdateContainerAsync<T>(T container, CancellationToken cancellationToken = default) where T : Container;
    Task DeleteContainerAsync(Guid id, CancellationToken cancellationToken = default);

    // ContentItem queries
    Task<ContentItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContentItem?> GetItemByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentItem>> GetItemsByContainerAsync(Guid containerId, string? discriminator = null, CancellationToken cancellationToken = default);
    Task<ContentItem> CreateItemAsync(ContentItem item, CancellationToken cancellationToken = default);
    Task<ContentItem> UpdateItemAsync(ContentItem item, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);

    // File queries
    Task<ContentFile?> GetContentFileByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ContentFile> CreateContentFileAsync(ContentFile file, CancellationToken cancellationToken = default);

    // Classification queries
    Task<ClassifiedItem?> GetClassifiedItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClassifiedItem?> GetClassifiedByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassifiedItem>> GetClassifiedItemsByLabelAsync(Guid labelId, int offset = 0, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassifiedItem>> GetRecentClassifiedItemsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<ClassifiedItem> UpsertClassifiedItemAsync(ClassifiedItem item, CancellationToken cancellationToken = default);
    Task<ClassifiedItem> UpdateClassifiedItemAsync(ClassifiedItem item, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetSignalCountsAsync(CancellationToken cancellationToken = default);
    Task DeleteClassifiedItemAsync(Guid id, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ContentItem> GetUnclassifiedItemsAsync(int limit, CancellationToken cancellationToken = default);

    // Label queries
    Task<Label?> GetLabelByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Label> CreateLabelAsync(Label label, CancellationToken cancellationToken = default);
    Task AddLabelToContentAsync(Guid contentId, LabelName label, CancellationToken cancellationToken = default);
    Task AddLabelToItemAsync(Guid itemId, LabelName label, CancellationToken cancellationToken = default);

    // Relationships
    Task<ContentRelationship> CreateContentRelationshipAsync(ContentRelationship relationship, CancellationToken cancellationToken = default);

    // Metrics
    Task<MetricDefinition?> GetMetricDefinitionByNameAsync(string name, CancellationToken cancellationToken = default);
}
