using System.Runtime.CompilerServices;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.Tests.TestDoubles;

internal sealed class InMemoryWorkplaceIqStore : IWorkplaceIqStore
{
    public List<DiscussionContent> DiscussionContents { get; } = [];
    public List<FolderContent> FolderContents { get; } = [];
    public List<FeedContent> FeedContents { get; } = [];
    public List<GroupContent> GroupContents { get; } = [];
    public List<ContentItem> Items { get; } = [];
    public List<ContentFile> ContentFiles { get; } = [];
    public List<Label> Labels { get; } = [];
    public List<ContentRelationship> ContentRelationships { get; } = [];
    public List<MetricDefinition> MetricDefinitions { get; } = [];
    public List<ClassifiedItem> ClassifiedItems { get; } = [];
    public List<ContentLabel> ContentLabels { get; } = [];
    public List<ContentItemLabel> ContentItemLabels { get; } = [];

    private List<T> GetContainerList<T>() where T : Container
    {
        if (typeof(T) == typeof(DiscussionContent)) return DiscussionContents as List<T> ?? [];
        if (typeof(T) == typeof(FolderContent)) return FolderContents as List<T> ?? [];
        if (typeof(T) == typeof(FeedContent)) return FeedContents as List<T> ?? [];
        if (typeof(T) == typeof(GroupContent)) return GroupContents as List<T> ?? [];
        if (typeof(T) == typeof(Container)) return GetAllContainers().Cast<T>().ToList();
        return [];
    }

    // ── Container CRUD ──────────────────────────────────────────────

    public Task<T?> GetContainerByIdAsync<T>(Guid id, CancellationToken ct = default) where T : Container
        => Task.FromResult(GetContainerList<T>().FirstOrDefault(c => c.Id == id));

    public Task<T?> GetContainerByNameAsync<T>(string name, CancellationToken ct = default) where T : Container
        => Task.FromResult(GetContainerList<T>().FirstOrDefault(c => c.Name == name));

    public Task<IReadOnlyList<T>> GetContainersByTypeAsync<T>(CancellationToken ct = default) where T : Container
        => Task.FromResult<IReadOnlyList<T>>(
            GetContainerList<T>().Where(c => c.Status != "archived").OrderBy(c => c.Title).ToList());

    public Task<IReadOnlyList<Container>> GetAllContainersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Container>>(GetAllContainers().Where(c => c.Status != "archived").OrderBy(c => c.Title).ToList());

    public Task<T> CreateContainerAsync<T>(T container, CancellationToken ct = default) where T : Container
    {
        if (typeof(T) == typeof(Container))
        {
            AddContainerByRuntimeType(container);
            return Task.FromResult(container);
        }
        GetContainerList<T>().Add(container);
        return Task.FromResult(container);
    }

    private void AddContainerByRuntimeType(Container container)
    {
        if (container is GroupContent gc) { GroupContents.Add(gc); return; }
        if (container is FeedContent fc) { FeedContents.Add(fc); return; }
        if (container is DiscussionContent dc) { DiscussionContents.Add(dc); return; }
        if (container is FolderContent flc) { FolderContents.Add(flc); return; }
    }

    public Task<T> UpdateContainerAsync<T>(T container, CancellationToken ct = default) where T : Container
    {
        var list = GetContainerList<T>();
        var index = list.FindIndex(c => c.Id == container.Id);
        if (index >= 0) list[index] = container;
        return Task.FromResult(container);
    }

    public Task DeleteContainerAsync(Guid id, CancellationToken ct = default)
    {
        foreach (var container in GetAllContainers().Where(c => c.Id == id))
        {
            container.Status = "archived";
        }
        return Task.CompletedTask;
    }

    // ── ContentItem CRUD ────────────────────────────────────────────

    public Task<ContentItem?> GetItemByNameAsync(string name, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(i => i.Name == name));

    public Task<ContentItem?> GetItemByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<IReadOnlyList<ContentItem>> GetItemsByContainerAsync(
        Guid containerId, string? discriminator = null, CancellationToken ct = default)
    {
        var query = Items.Where(i => i.ContainerId == containerId).Where(i => i.Status != "archived");
        if (!string.IsNullOrWhiteSpace(discriminator))
            query = query.Where(i => i.Discriminator == discriminator);
        return Task.FromResult<IReadOnlyList<ContentItem>>(query.OrderByDescending(i => i.CreatedAt).ToList());
    }

    public Task<ContentItem> CreateItemAsync(ContentItem item, CancellationToken ct = default)
    { Items.Add(item); return Task.FromResult(item); }

    public Task<ContentItem> UpdateItemAsync(ContentItem item, CancellationToken ct = default)
    {
        var index = Items.FindIndex(i => i.Id == item.Id);
        if (index >= 0) Items[index] = item;
        return Task.FromResult(item);
    }

    public Task DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = Items.FirstOrDefault(i => i.Id == id);
        if (item is not null) item.Status = "archived";
        return Task.CompletedTask;
    }

    // ── File CRUD ───────────────────────────────────────────────────

    public Task<ContentFile?> GetContentFileByItemIdAsync(Guid itemId, CancellationToken ct = default)
        => Task.FromResult(ContentFiles.FirstOrDefault(f => f.Id == itemId));

    public Task<ContentFile> CreateContentFileAsync(ContentFile file, CancellationToken ct = default)
    { ContentFiles.Add(file); return Task.FromResult(file); }

    // ── Classification ──────────────────────────────────────────────

    public Task<ClassifiedItem?> GetClassifiedItemByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(ClassifiedItems.FirstOrDefault(item => item.Id == id));

    public Task<ClassifiedItem?> GetClassifiedByContentIdAsync(Guid contentId, CancellationToken ct = default)
        => Task.FromResult(ClassifiedItems.FirstOrDefault(item => item.ContentId == contentId));

    public Task<IReadOnlyList<ClassifiedItem>> GetClassifiedItemsByLabelAsync(
        Guid labelId, int offset = 0, int limit = 50, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ClassifiedItem>>(
            ClassifiedItems.Where(item => item.LabelId == labelId)
                .OrderByDescending(item => item.ClassifiedAt).Skip(offset).Take(limit).ToList());

    public Task<IReadOnlyList<ClassifiedItem>> GetRecentClassifiedItemsAsync(int limit = 20, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ClassifiedItem>>(
            ClassifiedItems.Where(item => !item.IsNoise)
                .OrderByDescending(item => item.ClassifiedAt).Take(limit).ToList());

    public Task<ClassifiedItem> UpsertClassifiedItemAsync(ClassifiedItem item, CancellationToken ct = default)
    {
        var existing = ClassifiedItems.FirstOrDefault(ci => ci.ContentId == item.ContentId);
        if (existing is not null)
        {
            existing.LabelId = item.LabelId;
            existing.Reasoning = item.Reasoning;
            existing.IsNoise = item.IsNoise;
            existing.AttemptCount = item.AttemptCount;
            existing.HallucinatedSignal = item.HallucinatedSignal;
            existing.Embedding = item.Embedding;
            existing.ClassificationSource = item.ClassificationSource;
            existing.ClassifiedAt = item.ClassifiedAt;
            return Task.FromResult(existing);
        }
        ClassifiedItems.Add(item);
        return Task.FromResult(item);
    }

    public Task<ClassifiedItem> UpdateClassifiedItemAsync(ClassifiedItem item, CancellationToken ct = default)
    {
        var index = ClassifiedItems.FindIndex(c => c.Id == item.Id);
        if (index >= 0) ClassifiedItems[index] = item;
        return Task.FromResult(item);
    }

    public Task<Dictionary<Guid, int>> GetSignalCountsAsync(CancellationToken ct = default)
        => Task.FromResult(ClassifiedItems.GroupBy(item => item.LabelId).ToDictionary(g => g.Key, g => g.Count()));

    public Task DeleteClassifiedItemAsync(Guid id, CancellationToken ct = default)
    { ClassifiedItems.RemoveAll(c => c.Id == id); return Task.CompletedTask; }

    public async IAsyncEnumerable<ContentItem> GetUnclassifiedItemsAsync(int limit, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var classifiedIds = new HashSet<Guid>(ClassifiedItems.Select(item => item.ContentId));
        var unclassified = Items.Where(i => i.Status != "archived")
            .Where(i => !classifiedIds.Contains(i.Id))
            .OrderByDescending(i => i.CreatedAt).Take(limit).ToList();
        foreach (var item in unclassified) yield return item;
    }

    // ── Labels ──────────────────────────────────────────────────────

    public Task<Label?> GetLabelByNameAsync(string name, CancellationToken ct = default)
        => Task.FromResult(Labels.FirstOrDefault(l =>
            string.Equals(l.NormalizedName, name, StringComparison.OrdinalIgnoreCase)));

    public Task<Label> CreateLabelAsync(Label label, CancellationToken ct = default)
    { Labels.Add(label); return Task.FromResult(label); }

    public Task AddLabelToContentAsync(Guid contentId, LabelName label, CancellationToken ct = default)
    {
        var content = GetAllContainers().FirstOrDefault(c => c.Id == contentId);
        if (content is null) return Task.CompletedTask;
        var labelEntity = GetOrCreateLabel(label);
        ContentLabels.Add(new ContentLabel { ContentId = content.Id, Content = content, LabelId = labelEntity.Id, Label = labelEntity });
        return Task.CompletedTask;
    }

    public Task AddLabelToItemAsync(Guid itemId, LabelName label, CancellationToken ct = default)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return Task.CompletedTask;
        var labelEntity = GetOrCreateLabel(label);
        var itemLabel = new ContentItemLabel { ContentItemId = item.Id, ContentItem = item, LabelId = labelEntity.Id, Label = labelEntity };
        ContentItemLabels.Add(itemLabel);
        item.Labels.Add(itemLabel);
        return Task.CompletedTask;
    }

    private Label GetOrCreateLabel(LabelName labelName)
    {
        var label = Labels.FirstOrDefault(candidate => candidate.NormalizedName == labelName.NormalizedName);
        if (label is not null) return label;
        label = new Label { Name = labelName.Name, NormalizedName = labelName.NormalizedName, Slug = labelName.Slug };
        Labels.Add(label);
        return label;
    }

    private IEnumerable<Container> GetAllContainers()
    {
        foreach (var c in DiscussionContents) yield return c;
        foreach (var c in FolderContents) yield return c;
        foreach (var c in FeedContents) yield return c;
        foreach (var c in GroupContents) yield return c;
    }

    // ── Relationships ───────────────────────────────────────────────

    public Task<ContentRelationship> CreateContentRelationshipAsync(ContentRelationship relationship, CancellationToken ct = default)
    {
        relationship.SourceContent = GetAllContainers().FirstOrDefault(c => c.Id == relationship.SourceContentId);
        relationship.TargetContent = GetAllContainers().FirstOrDefault(c => c.Id == relationship.TargetContentId);
        ContentRelationships.Add(relationship);
        relationship.SourceContent?.SourceRelationships.Add(relationship);
        relationship.TargetContent?.TargetRelationships.Add(relationship);
        return Task.FromResult(relationship);
    }

    // ── Metrics ─────────────────────────────────────────────────────

    public Task<MetricDefinition?> GetMetricDefinitionByNameAsync(string name, CancellationToken ct = default)
        => Task.FromResult(MetricDefinitions.FirstOrDefault(m => m.Name == name));
}
