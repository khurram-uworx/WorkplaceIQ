# Proposals

> Design decisions and implementation plans for evolving WorkplaceIQ based on SignalFlow learnings. Each proposal links back to [OBSERVATIONS.md](./OBSERVATIONS.md) and [POTENTIAL-AI-FEATURES.md](./POTENTIAL-AI-FEATURES.md).

---

## Proposal 1: Vector Store Metadata Filtering

**Addresses:** OBSERVATIONS §1 (Missing Classification Data Model), §5 (No Vector/Embedding Abstractions), §10 (Re-invented Patterns — `MoreLikeAsync` in-memory similarity)
**Supports:** POTENTIAL-AI-FEATURES P0 (`IVectorStore`), P1 (`IFeedbackService` — `MoreLikeAsync`)

### Problem

`IVectorStore.SearchAsync` currently takes only `(embedding, topK)` with no filter. This means:

1. **`MoreLikeAsync`** (`FeedbackService.cs:79-100`) cannot delegate to the vector store — it loads every `ClassifiedItem` into memory and computes cosine similarity in C#. O(n) memory, O(n) CPU — not viable beyond ~10K items.
2. **`VectorClassifier.ClassifyAsync`** (`VectorClassifier.cs:102-107`) searches across ALL vector entries, including noise items and items from unrelated signals. This dilutes neighbor relevance.
3. **No scoped similarity search.** You cannot ask "find items similar to this one, but only within AI/ML" or "find items similar to this one that are not noise."

### Proposed Solution

Add metadata fields to vector store entries and an optional `SearchFilter` parameter on `SearchAsync`.

#### Vector Entry Metadata

```csharp
public sealed record VectorEntry
{
    public string Id { get; init; } = string.Empty;
    public string ContentId { get; init; } = string.Empty;       // FK → Content
    public string Signal { get; init; } = string.Empty;          // for signal filtering
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public bool IsNoise { get; init; }                           // exclude noise from searches
    public DateTimeOffset ClassifiedAt { get; init; }            // for recency filtering
    public ReadOnlyMemory<float> Embedding { get; init; }        // the vector
}
```

These fields already exist in the current `VectorIndexEntry`. The change is that `SearchAsync` can **filter on them**.

#### SearchFilter

```csharp
public sealed record SearchFilter
{
    /// <summary>Only return entries with this signal value.</summary>
    public string? SignalEquals { get; init; }

    /// <summary>Exclude entries marked as noise.</summary>
    public bool? ExcludeNoise { get; init; }

    /// <summary>Only return entries classified after this date.</summary>
    public DateTimeOffset? ClassifiedAfter { get; init; }

    /// <summary>Only return entries whose ContentId is in this set.</summary>
    public IReadOnlySet<string>? ContentIdIn { get; init; }
}
```

#### Updated IVectorStore

```csharp
public interface IVectorStore
{
    Task UpsertAsync(VectorEntry entry, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<VectorEntry> entries, CancellationToken ct = default);

    Task<IReadOnlyList<SearchHit>> SearchAsync(
        ReadOnlyMemory<float> embedding,
        int topK,
        SearchFilter? filter = null,
        CancellationToken ct = default);

    Task<long> CountAsync(CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}

public sealed record SearchHit(VectorEntry Entry, float Score);
```

### Impact

#### What improves

| Pattern | Before | After |
|---|---|---|
| `MoreLikeAsync(itemId, topK)` | Load ALL embeddings, compute cosine in C# | `store.SearchAsync(embedding, topK)` — vector store handles it |
| `MoreLikeAsync(itemId, topK, "AI/ML")` | Not possible without loading all + filtering in memory | `store.SearchAsync(embedding, topK, new SearchFilter { SignalEquals = "AI/ML" })` |
| `VectorClassifier.ClassifyAsync` neighbor search | Searches all entries including noise | `SearchFilter { ExcludeNoise = true }` |
| Find similar across multiple signals | Manual UNION of filtered loads | `SearchFilter { ContentIdIn = signalContentIds }` |

#### What stays the same

- `InMemoryVectorStore` still works — the filter is applied client-side after the search (acceptable for dev scale).
- The `VectorEntry` model already has these fields — no schema migration needed.
- `EmbeddingService`, `CategoryCentroidTracker`, `VectorClassifier` logic unchanged.

#### What gets removed

- `FeedbackService.MoreLikeAsync` in-memory loop → delegate to `store.SearchAsync` with the target item's embedding and optional filter.
- `ClassifiedItem.Embedding byte[]` column (future: after pgvector store is production) — the vector store becomes the sole vector source.

### Backend Implementation Notes

#### InMemoryVectorStore

```csharp
public async Task<IReadOnlyList<SearchHit>> SearchAsync(
    ReadOnlyMemory<float> embedding, int topK,
    SearchFilter? filter, CancellationToken ct)
{
    var candidates = store.Values.AsEnumerable();

    if (filter?.ExcludeNoise == true)
        candidates = candidates.Where(e => !e.IsNoise);
    if (filter?.SignalEquals is not null)
        candidates = candidates.Where(e => e.Signal == filter.SignalEquals);
    if (filter?.ClassifiedAfter is not null)
        candidates = candidates.Where(e => e.ClassifiedAt >= filter.ClassifiedAfter.Value);
    if (filter?.ContentIdIn is not null)
        candidates = candidates.Where(e => filter.ContentIdIn.Contains(e.ContentId));

    // Compute similarity and take topK
    var scored = candidates
        .Select(e => (e, TensorPrimitives.CosineSimilarity(
            embedding.Span, e.Embedding.Span)))
        .OrderByDescending(x => x.Item2)
        .Take(topK)
        .Select(x => new SearchHit(x.e, x.Item2))
        .ToList();

    return scored;
}
```

#### pgvector Implementation (future)

```sql
SELECT content_id, signal, title, summary, is_noise, classified_at,
       1 - (embedding <=> @queryEmbedding) AS similarity
FROM vector_entries
WHERE (@signalEquals IS NULL OR signal = @signalEquals)
  AND (@excludeNoise = FALSE OR is_noise = FALSE)
  AND (@classifiedAfter IS NULL OR classified_at >= @classifiedAfter)
ORDER BY embedding <=> @queryEmbedding
LIMIT @topK;
```

### Migration Path

1. Add `SearchFilter` record and update `IVectorStore.SearchAsync` signature (add `filter` parameter with default `null`).
2. Update `InMemoryVectorStore.SearchAsync` to apply filters.
3. Update callers: `VectorClassifier.ClassifyAsync` passes `ExcludeNoise = true`.
4. Refactor `FeedbackService.MoreLikeAsync` to delegate to vector store.
5. Future: Add optional `ClassifiedAt` to `VectorEntry` and populate on write.
6. Future: After pgvector is available, drop `ClassifiedItem.Embedding` column.

### Open Questions

- Should `SearchFilter` be an abstract class or a `record`? Record is preferred for value semantics.
- Should the vector store support `AND` vs `OR` filter combinations? Start with ALL-AND (all filters must match), add `OR` if needed.
- Should `ExcludeNoise` be `true` by default? Probably yes — noise exclusion is the common case.
