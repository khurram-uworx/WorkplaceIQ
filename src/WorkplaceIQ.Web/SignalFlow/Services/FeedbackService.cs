using System.Numerics.Tensors;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public class FeedbackService(
    IWorkplaceIqStore store,
    IVectorStore vectorStore) : IFeedbackService
{
    public async Task<List<SignalGroup>> GetSignalsAsync(CancellationToken ct = default)
    {
        var byLabel = await store.GetSignalCountsAsync(ct);
        var groups = new List<SignalGroup>();

        foreach (var (labelId, count) in byLabel)
        {
            var items = await store.GetClassifiedItemsByLabelAsync(labelId, limit: count, cancellationToken: ct);
            var label = items.FirstOrDefault()?.SignalLabel;
            groups.Add(new SignalGroup
            {
                Signal = label?.Name ?? "Unknown",
                Count = count,
                Items = items.ToList()
            });
        }

        return groups.OrderByDescending(g => g.Count).ToList();
    }

    public Task<IReadOnlyList<ClassifiedItem>> GetRecentItemsAsync(int limit = 20, CancellationToken ct = default)
    {
        return store.GetRecentClassifiedItemsAsync(limit, ct);
    }

    public async Task<List<ClassifiedItem>> GetNoiseAsync(CancellationToken ct = default)
    {
        var all = await store.GetRecentClassifiedItemsAsync(int.MaxValue, ct);
        return all.Where(i => i.IsNoise).OrderByDescending(i => i.ClassifiedAt).ToList();
    }

    public async Task<List<ClassifiedItem>> GetBouncedAsync(CancellationToken ct = default)
    {
        var recent = await store.GetRecentClassifiedItemsAsync(int.MaxValue, ct);
        return recent.Where(i => i.AttemptCount >= 5).OrderByDescending(i => i.ClassifiedAt).ToList();
    }

    public Task<ClassifiedItem?> GetItemDetailsAsync(Guid itemId, CancellationToken ct = default)
    {
        return store.GetClassifiedItemByIdAsync(itemId, ct);
    }

    public async Task<bool> ReclassifyAsync(Guid itemId, string newSignal, bool isNoise, CancellationToken ct = default)
    {
        var item = await store.GetClassifiedItemByIdAsync(itemId, ct);
        if (item is null) return false;

        var label = await store.GetLabelByNameAsync(newSignal, ct);
        if (label is null) return false;

        item.LabelId = label.Id;
        item.IsNoise = isNoise;
        item.ClassifiedAt = DateTimeOffset.UtcNow;
        await store.UpdateClassifiedItemAsync(item, ct);
        return true;
    }

    public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await store.GetClassifiedItemByIdAsync(itemId, ct);
        if (item is null) return false;

        item.IsNoise = true;
        await store.UpdateClassifiedItemAsync(item, ct);
        return true;
    }

    public async Task<List<(ClassifiedItem Item, double Score)>> MoreLikeAsync(
        Guid classifiedId, int top = 6, CancellationToken ct = default)
    {
        var target = await store.GetClassifiedItemByIdAsync(classifiedId, ct);
        if (target?.Embedding is null || target.Embedding.Length == 0)
            return [];

        var targetVec = EmbeddingSerializer.FromBytes(target.Embedding);
        var candidates = await store.GetRecentClassifiedItemsAsync(int.MaxValue, ct);
        candidates = candidates.Where(c => c.Embedding is not null && c.Embedding.Length > 0 && c.Id != classifiedId).ToList();

        var scores = new List<(ClassifiedItem Item, double Score)>();
        foreach (var c in candidates)
        {
            if (c.Embedding is null || c.Embedding.Length == 0) continue;
            var vec = EmbeddingSerializer.FromBytes(c.Embedding);
            var sim = TensorPrimitives.CosineSimilarity(targetVec.Span, vec.Span);
            scores.Add((c, sim));
        }

        return scores.OrderByDescending(x => x.Score).Take(top).ToList();
    }

    public async Task<Dictionary<string, int>> GetSignalCountsAsync(CancellationToken ct = default)
    {
        var labelCounts = await store.GetSignalCountsAsync(ct);
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (labelId, count) in labelCounts)
        {
            var items = await store.GetClassifiedItemsByLabelAsync(labelId, limit: 1, cancellationToken: ct);
            var name = items.FirstOrDefault()?.SignalLabel?.Name ?? labelId.ToString();
            result[name] = count;
        }
        return result;
    }

    public async Task<int> GetNoiseCountAsync(CancellationToken ct = default)
    {
        var all = await store.GetRecentClassifiedItemsAsync(int.MaxValue, ct);
        return all.Count(i => i.IsNoise);
    }

    public async Task<int> GetFailedCountAsync(CancellationToken ct = default)
    {
        var all = await store.GetRecentClassifiedItemsAsync(int.MaxValue, ct);
        return all.Count(i => i.AttemptCount >= 5);
    }

    public async Task<bool> MarkNotNoiseAsync(Guid classifiedId, CancellationToken ct = default)
    {
        var item = await store.GetClassifiedItemByIdAsync(classifiedId, ct);
        if (item is null) return false;

        item.IsNoise = false;
        item.ClassifiedAt = DateTimeOffset.UtcNow;
        await store.UpdateClassifiedItemAsync(item, ct);
        return true;
    }

    public async Task<(bool Success, ClassifiedItem? Item)> RetryFailedAsync(
        Guid classifiedId, CancellationToken ct = default)
    {
        var item = await store.GetClassifiedItemByIdAsync(classifiedId, ct);
        if (item?.RssItem is null)
            return (false, null);

        item.AttemptCount = 0;
        item.HallucinatedSignal = null;
        await store.UpdateClassifiedItemAsync(item, ct);

        return (true, item);
    }
}
