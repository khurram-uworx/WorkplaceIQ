# Potential AI Features

> **Context:** SignalFlow (`src/WorkplaceIQ.Web/SignalFlow/`) is a real-world AI Data Engineering pipeline implemented on WorkplaceIQ. This document catalogs the patterns, abstractions, and services that SignalFlow built — each of which is a candidate for extraction into WorkplaceIQ's platform "intelligence layer."
>
> Priority levels: **P0** = core AI primitive, **P1** = important AI infrastructure, **P2** = useful pattern, **P3** = nice-to-have model.

---

## P0: `IVectorStore` — Vector Storage Abstraction

**Prerequisite:** [Proposal 1 — Vector Store Metadata Filtering](./PROPOSALS.md#proposal-1-vector-store-metadata-filtering) defines the `SearchFilter` design and metadata fields on vector entries. This section assumes that proposal is accepted.

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/IVectorStore.cs` + `InMemoryVectorStore.cs`

### Current Form
An interface with these key methods:
```csharp
Task UpsertAsync(VectorIndexEntry entry, CancellationToken ct);
Task UpsertBatchAsync(IEnumerable<VectorIndexEntry> entries, CancellationToken ct);
Task<IAsyncEnumerable<(VectorIndexEntry Record, float Score)>> SearchAsync(
    ReadOnlyMemory<float> embedding, int topK, CancellationToken ct);
Task<long> CountAsync(CancellationToken ct);
Task ClearAsync(CancellationToken ct);
```

Implemented once (`InMemoryVectorStore` wrapping `Microsoft.SemanticKernel.Connectors.InMemory`). Not persisted across app restarts.

### What to Extract
- Move `IVectorStore` to `WorkplaceIQ.AI.Abstractions`.
- Add a `pgvector` implementation using Npgsql's vector support (`CREATE EXTENSION vector`, `halfvec` type).
- Consider a provider model: `AddVectorStore<TImplementation>()` with DI registration for InMemory (dev) and pgvector (prod).
- Simplify the `SearchAsync` return type: `IAsyncEnumerable<(TEntry Record, float Score)>` is awkward. `IReadOnlyList<SearchHit>` is cleaner.
- **Add `SearchFilter` support** (per Proposal 1) so callers can scope searches by signal, exclude noise, etc.

### API Surface (Proposed)

```csharp
// WorkplaceIQ.AI.Abstractions
public sealed record SearchFilter
{
    public string? SignalEquals { get; init; }
    public bool? ExcludeNoise { get; init; }
    public DateTimeOffset? ClassifiedAfter { get; init; }
    public IReadOnlySet<string>? ContentIdIn { get; init; }
}

public sealed record VectorEntry(
    string Id,
    string ContentId,
    string Signal,
    string Title,
    string Summary,
    bool IsNoise,
    DateTimeOffset ClassifiedAt,
    ReadOnlyMemory<float> Embedding);

public sealed record SearchHit(
    VectorEntry Entry,
    float Score);

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
```

### Migration Notes

- `ClassifiedItem.Embedding byte[]` is temporary. Once pgvector is production, the column can be dropped — the vector store becomes the sole vector source.
- The `VectorEntry` metadata fields (`Signal`, `IsNoise`, `ClassifiedAt`) are already populated by `PipelineOrchestrator.PersistResultAsync` — they just need to be plumbed into the store.
- `InMemoryVectorStore` applies filters client-side after search (acceptable for dev scale). pgvector applies them via SQL `WHERE` clauses for index-assisted filtering.

---

## P0: `IEmbeddingService` — Embedding Generation Abstraction

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/EmbeddingService.cs`

### Current Form
A concrete class wrapping `IEmbeddingGenerator<string, Embedding<float>>` with:
- 8000-character text truncation before embedding
- `RssItem`-to-text composition: `$"{item.Title}\n{item.Summary}"`
- `ConfigureAwait(false)` on all async calls

```csharp
public async Task<ReadOnlyMemory<float>> GenerateAsync(RssItem item, CancellationToken ct = default)
{
    var text = $"{item.Title}\n{item.Summary ?? string.Empty}";
    if (text.Length > 8000) text = text[..8000];
    var embedding = await generator.GenerateAsync(text, cancellationToken: ct).ConfigureAwait(false);
    return embedding.First().Vector;
}
```

### What to Extract
- Move to `WorkplaceIQ.AI.Abstractions` as `IEmbeddingService` with overloads for common inputs.
- Add a `GenerateAsync(string text)` overload for free-form text.
- Add a `GenerateAsync<T>(T item, Func<T, string> textSelector)` overload for typed items.
- Add truncation as a configurable option, not a hard-coded constant.
- Add retry / fallback: if the primary embedding model fails, try a secondary model.
- Make `EmbeddingService` a platform service auto-registered via `AddWorkplaceIQAI()`.

### API Surface (Proposed)

```csharp
// WorkplaceIQ.AI.Abstractions
public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken ct = default);
    Task<ReadOnlyMemory<float>> GenerateAsync<T>(T item, Func<T, string> textSelector, CancellationToken ct = default);
}
```

---

## P0: Core Classification Types

### Source
- `src/WorkplaceIQ.Web/SignalFlow/Models/ClassificationResult.cs`
- `src/WorkplaceIQ.Web/SignalFlow/Models/ClassificationDecision.cs`
- `src/WorkplaceIQ.Web/SignalFlow/Models/NeighborStats.cs`
- `src/WorkplaceIQ/Content/ClassifiedItem.cs` (the `ClassificationSources` constants)

### Current Form
Three records and a static constants class:

```csharp
// ClassificationResult — structured LLM output
public sealed record ClassificationResult
{
    public string Signal { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public bool IsNoise { get; init; }
    public string? HallucinatedSignal { get; init; }
}

// ClassificationDecision — pipeline outcome
public sealed record ClassificationDecision
{
    public ClassificationResult Result { get; init; } = new();
    public string Source { get; init; } = string.Empty;
    public NeighborStats? Stats { get; init; }
    public bool WasAutoLabelled { get; init; }
}

// NeighborStats — vector search gating
public sealed record NeighborStats { ... }

// ClassificationSources
public static class ClassificationSources
{
    public const string Bootstrap = "Bootstrap";
    public const string VectorAuto = "VectorAuto";
    public const string LlmSparseNeighbors = "LlmSparseNeighbors";
    public const string LlmLowConfidence = "LlmLowConfidence";
    public const string LlmEmbeddingFailed = "LlmEmbeddingFailed";
    public const string Failed = "Failed";
}
```

### What to Extract
- Move to `WorkplaceIQ.AI.Models` as proper types.
- Convert `ClassificationSources` to `enum ClassificationSource { Bootstrap, VectorAuto, LlmSparseNeighbors, LlmLowConfidence, LlmEmbeddingFailed, Failed }`.
- Add EF Core value converter for the enum to string column.
- Make `NeighborStats.FromInline()` / `NeighborStats.Empty()` part of the platform API.
- Add JSON serialization attributes for SignalR / API use.

### API Surface (Proposed)

```csharp
// WorkplaceIQ.AI.Models
public enum ClassificationSource
{
    Bootstrap,
    VectorAuto,
    LlmSparseNeighbors,
    LlmLowConfidence,
    LlmEmbeddingFailed,
    Failed
}
```

---

## P1: `VectorClassifier` — Multi-Strategy Hybrid Classifier

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/VectorClassifier.cs` (171 lines)

### Current Form
A concrete class implementing a 3-tier hybrid classification strategy:

1. **Bootstrap** (below `bootstrapThreshold` = 20 classified items): Direct LLM fallback. No vector search yet.
2. **Vector Auto** (above threshold, high confidence): kNN search → confidence gates (min neighbors, min agreement, min similarity, min margin) → centroid agreement check → `VectorAuto` classification.
3. **LLM Fallback** (sparse neighbors or low confidence): When vector search yields too few neighbors (`< minNeighbors`) or fails gate checks, falls back to LLM via `LlmFallbackDelegate`.

Configurable gates: `topK=10`, `minNeighbors=5`, `minNeighborAgreement=5`, `minAvgSimilarity=0.86`, `minMargin=0.10`.

### What to Extract
- Refactor into a strategy pattern: `IClassificationStrategy` with implementations:
  - `VectorClassificationStrategy` — pure vector-kNN with confidence gates
  - `LlmClassificationStrategy` — pure LLM classification
  - `HybridClassificationStrategy` — the multi-tier strategy with fallback
- Extract `LlmFallbackDelegate` as an `IChatClient`-based default implementation.
- Move gate parameters into a `ClassificationOptions` record: `BootstrapThreshold`, `TopK`, `MinNeighbors`, `MinNeighborAgreement`, `MinAvgSimilarity`, `MinMargin`.
- Inject via DI: `services.AddHybridClassifier(options => { ... })`.

### API Surface (Proposed)

```csharp
// WorkplaceIQ.AI.Classifiers
public sealed record ClassificationOptions
{
    public int BootstrapThreshold { get; init; } = 20;
    public int TopK { get; init; } = 10;
    public int MinNeighbors { get; init; } = 5;
    public int MinNeighborAgreement { get; init; } = 5;
    public double MinAvgSimilarity { get; init; } = 0.86;
    public double MinMargin { get; init; } = 0.10;
}

public interface IClassificationStrategy
{
    Task<ClassificationDecision> ClassifyAsync(
        string title, string text, ReadOnlyMemory<float> embedding,
        int totalClassifiedCount, CancellationToken ct = default);
}
```

---

## P1: `CategoryCentroidTracker` — Incremental Centroid Computation

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/CategoryCentroidTracker.cs` (121 lines)

### Current Form
Thread-safe, incremental running-mean centroid tracker per category (signal):
- `AddOrUpdate(signal, embedding)` — O(1) update using Welford's online algorithm
- `GetCentroidSimilarity(signal, embedding)` — cosine similarity between embedding and signal's centroid
- `GetBestCentroidMatch(embedding, minSimilarity)` — find best-matching signal across all centroids
- Thread-safe via `ConcurrentDictionary` with per-centroid locking
- Dimensionality validation on first update to prevent silent corruption

### What to Extract
- Move to `WorkplaceIQ.AI` as `ICentroidTracker` interface with `InMemoryCentroidTracker` implementation.
- Add `RedisCentroidTracker` for distributed scenarios (multiple app instances).
- Add centroid persistence: serialize centroids to DB so they survive restarts (currently rebuilt from `ClassifiedItem` table on each pipeline start via `RestoreVectorStateAsync`).
- Register as singleton: `services.AddSingleton<ICentroidTracker, InMemoryCentroidTracker>()`.

### API Surface (Proposed)

```csharp
// WorkplaceIQ.AI
public interface ICentroidTracker
{
    void AddOrUpdate(string category, ReadOnlyMemory<float> embedding);
    float? GetCentroidSimilarity(string category, ReadOnlyMemory<float> embedding);
    (string Category, float Score)? GetBestCentroidMatch(
        ReadOnlyMemory<float> embedding, float minSimilarity = 0f);
    IReadOnlyDictionary<string, CentroidState> GetAll();
}

public sealed record CentroidState(
    string Category,
    int Count,
    ReadOnlyMemory<float> Centroid);
```

---

## P1: `IFeedbackService` — Human-in-the-Loop Pattern

### Source
- `src/WorkplaceIQ.Web/SignalFlow/Services/IFeedbackService.cs`
- `src/WorkplaceIQ.Web/SignalFlow/Services/FeedbackService.cs` (151 lines)

### Current Form
A service that provides both read queries (dashboard data) and write operations (feedback actions):

```csharp
public interface IFeedbackService
{
    // Read queries
    Task<List<SignalGroup>> GetSignalsAsync(CancellationToken ct);
    Task<IReadOnlyList<ClassifiedItem>> GetRecentItemsAsync(int limit, CancellationToken ct);
    Task<List<ClassifiedItem>> GetNoiseAsync(CancellationToken ct);
    Task<List<ClassifiedItem>> GetBouncedAsync(CancellationToken ct);
    Task<ClassifiedItem?> GetItemDetailsAsync(Guid itemId, CancellationToken ct);
    Task<Dictionary<string, int>> GetSignalCountsAsync(CancellationToken ct);
    Task<int> GetNoiseCountAsync(CancellationToken ct);
    Task<int> GetFailedCountAsync(CancellationToken ct);

    // Write operations
    Task<bool> ReclassifyAsync(Guid itemId, string newSignal, bool isNoise, CancellationToken ct);
    Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct);
    Task<bool> MarkNotNoiseAsync(Guid classifiedId, CancellationToken ct);
    Task<(bool Success, ClassifiedItem? Item)> RetryFailedAsync(Guid classifiedId, CancellationToken ct);
    Task<List<(ClassifiedItem Item, double Score)>> MoreLikeAsync(Guid classifiedId, int top, CancellationToken ct);
}
```

### What to Extract
- Split into `IClassificationStore` (read queries) and `IClassificationFeedback` (write operations) for separation of concerns.
- Make the interface generic: `IClassificationFeedback<TEntity>` or parameterize by entity type.
- Extract `MoreLikeAsync` into a `ISimilaritySearch` service that works at the store level (not loading everything into memory).
- Move `RetryFailedAsync` logic (reset attempt count, re-queue for classification) into the job/pipeline infrastructure.

### Key Pattern: MoreLikeAsync

```csharp
// Current: loads ALL embeddings into memory
var candidates = await store.GetRecentClassifiedItemsAsync(int.MaxValue, ct);
foreach (var c in candidates)
{
    var sim = TensorPrimitives.CosineSimilarity(targetVec.Span, vec.Span);
    scores.Add((c, sim));
}
return scores.OrderByDescending(x => x.Score).Take(top).ToList();
```

This is O(n) memory and O(n) CPU. With 100K items, this allocates `100K × ClassifiedItem` objects and computes `100K` cosine similarities. The platform should delegate this to `IVectorStore.SearchAsync()`.

---

## P1: `EmbeddingSerializer` — Vector Serialization Utility

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/EmbeddingSerializer.cs` (30 lines)

### Current Form
Two static methods for `float[] ↔ byte[]` round-trip:
```csharp
public static byte[] ToBytes(ReadOnlyMemory<float> embedding)
    => MemoryMarshal.AsBytes(embedding.Span).ToArray();

public static ReadOnlyMemory<float> FromBytes(byte[] bytes)
    => MemoryMarshal.Cast<byte, float>(bytes).ToArray().AsMemory();
```

### What to Extract
- Move to `WorkplaceIQ.AI.Utilities` or make it a method on an `Embedding<T>` value type.
- Add null/empty handling (return `ReadOnlyMemory<float>.Empty` for null/empty byte arrays).
- Consider `Embedding<T>` struct that wraps `ReadOnlyMemory<float>` and handles serialization internally.

---

## P2: `PipelineBackgroundService` — Background Job Infrastructure

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/PipelineBackgroundService.cs` (113 lines)

### Current Form
A `BackgroundService` that:
- Accepts pipeline requests via a `Channel<PipelineRequest>` (bounded, single-reader, drop-write on overflow)
- Creates a DI scope per pipeline run
- Tracks `PipelineState(IsRunning, LastEvent?)` for reconnection recovery
- Uses `PipelineProgressReporter` to bridge `IProgress<PipelineEvent>` to SignalR broadcasts
- Handles `OperationCanceledException` and unexpected exceptions

### What to Extract

**Core components:**

1. **`JobQueue<TJob>`** — A channel-based queue with:
   - Single-reader guarantee
   - Backpressure via bounded channel
   - Optional DB persistence for crash recovery
   - `TryEnqueue(TJob) → bool` (returns false if queue full)
   - `GetState() → JobState` for monitoring

2. **`JobRunner<TJob, TEvent>`** — A `BackgroundService` base class:
   - Reads from `JobQueue`
   - Creates DI scopes
   - Manages cancellation tokens
   - Tracks running state
   - Reports progress via `IProgress<TEvent>`

3. **`ProgressToSignalRAdapter<T>`** — A reusable `IProgress<T>` → SignalR adapter:
   - Sends typed messages to `Clients.All` (or a specific group)
   - Fire-and-forget with proper exception handling (log, don't swallow)
   - Optionally stores last event for reconnection recovery

4. **Job event types** — Generic discriminated union:
   - `JobStarted<TStage>`
   - `JobProgress<TStage>`
   - `JobItemProcessed<TItem>`
   - `JobFailed<TStage>`
   - `JobCompleted`

### API Surface (Proposed)

```csharp
// WorkplaceIQ.Jobs
public interface IJobQueue<TJob>
{
    bool TryEnqueue(TJob job);
    JobState GetState();
}

public sealed record JobState(bool IsRunning, JobEvent? LastEvent);

public abstract class JobRunner<TJob, TEvent> : BackgroundService
{
    protected abstract IJobQueue<TJob> Queue { get; }
    protected abstract Task RunAsync(TJob job, IProgress<TEvent> progress, CancellationToken ct);
    // + OnCompleted, OnFailed virtual methods
}
```

---

## P2: `PipelineProgressReporter` — Progress to SignalR Adapter

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/PipelineBackgroundService.cs:95-112`

### Current Form
```csharp
sealed class PipelineProgressReporter(
    IHubContext<PipelineHub> hubContext,
    Action<PipelineEvent> onReport) : IProgress<PipelineEvent>
{
    public void Report(PipelineEvent value)
    {
        onReport(value);
        _ = hubContext.Clients.All.SendAsync("pipelineEvent", value, CancellationToken.None);
    }
}
```

### What to Extract
- Generic version: `ProgressReporter<TEvent>(IHubContext hub, string methodName, Action<TEvent> onReport)`.
- Add proper logging (not silent catch).
- Add optional group targeting (`Clients.Group(groupName)` instead of `Clients.All`).
- Add batching for high-frequency events.

---

## P2: `ConfigLoader` — Feature Configuration Pattern

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/ConfigLoader.cs` (106 lines)

### Current Form
A file-based config loader that reads markdown files from a directory and parses them into a `PipelineConfig`:
- `source.md` → feed sources
- `signals.md` → signal names
- `goal.md` → goal text
- `prompt.md` → prompt template
- `engine.md` → numeric/dictionary engine settings

### What to Extract
- `IFeatureConfiguration<TConfig>` interface with `LoadAsync(string path) → TConfig`.
- Multiple backend implementations:
  - `FileConfigProvider<TConfig>` — reads from file system
  - `JsonConfigProvider<TConfig>` — reads from JSON
  - `DbConfigProvider<TConfig>` — reads from DB
- Merge/override logic: env vars override file settings, file settings override defaults.
- Schema validation (JSON Schema or `System.ComponentModel.DataAnnotations`).

This is a medium-sized abstraction that would replace ad-hoc parsing across all WorkplaceIQ features.

---

## P2: `RssFetcher` — Content Connector Pattern

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/RssFetcher.cs` (50 lines)

### Current Form
A static class that fetches an RSS feed and yields `RssItem` records:
```csharp
public static async IAsyncEnumerable<RssItem> FetchFeedAsync(
    string url, string name, [EnumeratorCancellation] CancellationToken ct)
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var response = await client.GetAsync(url, ct);
    // ... SyndicationFeed.Load(xmlReader) ...
    foreach (var item in feed.Items)
        yield return new RssItem { ... ContentHash = sha256(...) };
}
```

### What to Extract
- `IContentConnector<TItem>` interface:
  ```csharp
  public interface IContentConnector<TItem>
  {
      string ConnectorType { get; } // "rss", "atom", "api", etc.
      IAsyncEnumerable<TItem> FetchAsync(CancellationToken ct = default);
  }
  ```
- `RssConnector` implementation using `IHttpClientFactory` (not raw `HttpClient`).
- Content hash / fingerprint as a platform primitive (`IContentHasher` with SHA256 implementation).
- `IConnectorRegistry` to register and enumerate connectors.
- `ConnectorOptions` for timeout, retry, user-agent configuration.

---

## P2: Pipeline Event Types

### Source
`src/WorkplaceIQ.Web/SignalFlow/Models/PipelineEvent.cs`

### Current Form
Polymorphic records with `[JsonDerivedType]` attributes for SignalR serialization:

```
PipelineEvent (base)
├── PipelineStarted(totalFeeds)
├── PipelineProgress(stage, current, total, message?)
├── PipelineItemProcessed(id, title, signal, isNoise, reasoning, hallucinatedSignal?)
├── PipelineFailed(stage, error, contentId?)
└── PipelineCompleted(totalItems, signalCount, noiseCount, failedCount, signalBreakdown?)
```

### What to Extract
- Generic job event types: `JobEvent` base with `JobStarted<TConfig>`, `JobProgress<TStage>`, `JobItemProcessed<TItem>`, `JobFailed`, `JobCompleted`.
- Keep `[JsonDerivedType]` serialization for SignalR compatibility.
- Move to `WorkplaceIQ.Jobs.Events` namespace.

---

## P3: Extended AI Models

### SignalGroup
`src/WorkplaceIQ.Web/SignalFlow/Models/SignalGroup.cs`
```csharp
public sealed record SignalGroup
{
    public string Signal { get; init; } = string.Empty;
    public int Count { get; init; }
    public List<ClassifiedItem> Items { get; init; } = [];
}
```
Extract to `WorkplaceIQ.AI.Models` as a general-purpose aggregate result type.

### VectorIndexEntry
`src/WorkplaceIQ.Web/SignalFlow/Models/VectorIndexEntry.cs`
Currently has `[VectorStoreKey]` and `[VectorStoreData]` attributes from `Microsoft.SemanticKernel`. If the platform provides `IVectorStore`, this becomes the platform's entry type.

### PipelineConfig
`src/WorkplaceIQ.Web/SignalFlow/Models/PipelineConfig.cs`
Extract to `WorkplaceIQ.AI.Configuration` as `AiPipelineConfig` or `ClassificationPipelineConfig`.
Current fields:
- `FeedSources`, `Goal`, `Signals`, `PromptTemplate`
- `Endpoint`, `ApiKey`, `EmbeddingModel`, `LlmModel`, `EmbeddingDimension`
- Classifier thresholds (`BootstrapThreshold`, `TopK`, `MinNeighbors`, etc.)

---

## P3: `PipelineRequest` — Job Request Model

### Source
`src/WorkplaceIQ.Web/SignalFlow/Models/PipelineRequest.cs`

```csharp
public sealed record PipelineRequest(
    string ConnectionId,
    PipelineConfig Config,
    CancellationToken RequestAborted);
```

Extract to a generic `JobRequest<TConfig>` record as part of the job infrastructure.

---

## Summary: What to Extract and In What Order

| Phase | What | Why Now |
|---|---|---|
| **1** | `ClassificationSource` enum | Required by any AI feature; trivial extraction |
| **1** | `ClassificationResult` / `ClassificationDecision` / `NeighborStats` to core models | Required by any AI feature; zero dependencies |
| **1** | `EmbeddingSerializer` utility | Required by any vector feature |
| **2** | `IVectorStore` + `InMemoryVectorStore` abstraction | Core AI primitive; needed by pgvector, similarity search |
| **2** | `IEmbeddingService` abstraction | Core AI primitive; needed by every AI pipeline |
| **2** | `ICentroidTracker` + `InMemoryCentroidTracker` | Needed for classification quality |
| **3** | `VectorClassifier` as strategy pattern | Flagship AI feature; proves the above abstractions work |
| **3** | `IFeedbackService` → split + extract | Production feedback loop |
| **4** | Job infrastructure (`JobQueue`, `JobRunner`, progress adapter) | Background processing for any AI or non-AI pipeline |
| **5** | Connector patterns (`RssConnector`, `IContentConnector`) | Reusable content ingestion |
| **5** | Configuration pattern (`IFeatureConfiguration`) | Replaces ad-hoc parsing across all features |

> **Bottom line:** The SignalFlow implementation contains ~3,000 lines of production-tested code that can be systematically extracted into WorkplaceIQ's platform. The P0 and P1 items are the highest-value extractions — they form the foundation for every AI feature the platform will support.
