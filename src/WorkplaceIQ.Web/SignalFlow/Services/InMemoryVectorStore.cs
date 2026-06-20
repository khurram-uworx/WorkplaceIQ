using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public sealed class InMemoryVectorStore : IVectorStore
{
    readonly List<VectorIndexEntry> entries = [];
    readonly object gate = new();

    public ValueTask<int> CountAsync(CancellationToken ct = default)
    {
        lock (gate) return ValueTask.FromResult(entries.Count);
    }

    public ValueTask UpsertAsync(VectorIndexEntry entry, CancellationToken ct = default)
    {
        lock (gate)
        {
            var existing = entries.FindIndex(e => e.Id == entry.Id);
            if (existing >= 0)
                entries[existing] = entry;
            else
                entries.Add(entry);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask UpsertBatchAsync(IEnumerable<VectorIndexEntry> batch, CancellationToken ct = default)
    {
        lock (gate)
        {
            foreach (var entry in batch)
            {
                var existing = entries.FindIndex(e => e.Id == entry.Id);
                if (existing >= 0)
                    entries[existing] = entry;
                else
                    entries.Add(entry);
            }
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> RemoveAsync(Guid rssItemId, CancellationToken ct = default)
    {
        lock (gate)
        {
            var idx = entries.FindIndex(e => e.RssItemId == rssItemId);
            if (idx >= 0)
            {
                entries.RemoveAt(idx);
                return ValueTask.FromResult(true);
            }
        }
        return ValueTask.FromResult(false);
    }

    public async IAsyncEnumerable<(VectorIndexEntry Record, double Score)> SearchAsync(
        ReadOnlyMemory<float> embedding, int topK, [EnumeratorCancellation] CancellationToken ct = default)
    {
        List<(VectorIndexEntry Record, double Score)> scored;
        lock (gate)
        {
            scored = entries
                .Where(e => !e.Embedding.IsEmpty)
                .Select(e =>
                {
                    var sim = TensorPrimitives.CosineSimilarity(embedding.Span, e.Embedding.Span);
                    return (e, double.IsNaN(sim) ? 0.0 : sim);
                })
                .OrderByDescending(x => x.Item2)
                .Take(topK)
                .ToList();
        }

        foreach (var hit in scored)
        {
            yield return hit;
        }
    }

    public ValueTask<IReadOnlyList<VectorIndexEntry>> GetAllAsync(CancellationToken ct = default)
    {
        lock (gate) return ValueTask.FromResult<IReadOnlyList<VectorIndexEntry>>(entries.ToList());
    }

    public ValueTask ClearAsync(CancellationToken ct = default)
    {
        lock (gate) entries.Clear();
        return ValueTask.CompletedTask;
    }
}
