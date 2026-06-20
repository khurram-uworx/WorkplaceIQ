using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public interface IVectorStore
{
    ValueTask<int> CountAsync(CancellationToken ct = default);
    ValueTask UpsertAsync(VectorIndexEntry entry, CancellationToken ct = default);
    ValueTask UpsertBatchAsync(IEnumerable<VectorIndexEntry> entries, CancellationToken ct = default);
    IAsyncEnumerable<(VectorIndexEntry Record, double Score)> SearchAsync(
        ReadOnlyMemory<float> embedding, int topK, CancellationToken ct = default);
    ValueTask<bool> RemoveAsync(Guid rssItemId, CancellationToken ct = default);
    ValueTask<IReadOnlyList<VectorIndexEntry>> GetAllAsync(CancellationToken ct = default);
    ValueTask ClearAsync(CancellationToken ct = default);
}
