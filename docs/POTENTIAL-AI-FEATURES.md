# Potential AI Features — Extraction & Platform Alignment

> **Context:** SignalFlow (`src/WorkplaceIQ.Web/SignalFlow/`) is a real-world AI Data Engineering pipeline on WorkplaceIQ. This document catalogs patterns, abstractions, and services worth extracting into WorkplaceIQ's platform "intelligence layer." Every extraction builds on Microsoft's official .NET AI stack: MEAI, MEVD, MEDI, and System.Numerics.Tensors.
>
> Priority levels: **P0** = core AI primitive, **P1** = important AI infrastructure, **P2** = useful pattern, **P3** = nice-to-have model.

---

## Platform Alignment

SignalFlow already aligns with Microsoft's .NET AI ecosystem:

| Pillar | Package | Used In | Status |
|---|---|---|---|
| **MEAI** | `Microsoft.Extensions.AI` | `EmbeddingService` (via `IEmbeddingGenerator`), chat via `IChatClient` | ✓ Used directly |
| **MEVD** | `Microsoft.Extensions.VectorData` (+ `*Connectors.InMemory`) | `VectorClassifier`, `FeedbackService`, `PipelineOrchestrator` via `VectorStore` | ✓ Used directly |
| **System.Numerics.Tensors** | `System.Numerics.Tensors` | `CategoryCentroidTracker` (`TensorPrimitives.CosineSimilarity`) | ✓ Used directly |

The two remaining platform pillars are adoption candidates:

| Pillar | Package | When |
|---|---|---|
| **MEDI** | `Microsoft.Extensions.DataIngestion` | When chunking/enrichment needs a formal pipeline |
| **MCP** | Model Context Protocol providers | When capabilities need to cross process boundaries |

---

## P0: MEVD VectorStore — Platform Alignment (Done)

### Current State

SignalFlow uses `VectorStore` (MEVD) directly — no custom `IVectorStore` wrapper. The collection type is `SignalFlowVectorEntry` with MEVD attributes (`[VectorStoreKey]`, `[VectorStoreData]`, `[VectorStoreVector]`). The in-memory provider (`Microsoft.SemanticKernel.Connectors.InMemory`) is used for development.

### What to Extract / Improve

- **Provider swap to pgvector**: Replace `InMemoryVectorStore` DI registration with `Microsoft.SemanticKernel.Connectors.PgVector` for production.
- **Dimension as config**: Currently hard-coded at 1536 in `SignalFlowVectorEntry`. Make configurable via `VectorStoreRecordCollectionDefinition` or a `VectorSchema.Create(dimension)` factory.
- **Drop `ClassifiedItem.Embedding byte[]`**: Once pgvector is production, the vector store is the sole embedding source; remove the redundant column.
- **Migration to `VectorStoreRecordCollection<TKey, TRecord>`**: MEVD's newer collection API provides richer functionality; validate feature parity.

### Key Points

- No custom vector store abstraction needed — MEVD is the standard .NET abstraction.
- `VectorSearchOptions<TRecord>.Filter` provides LINQ-based filtering (no custom `SearchFilter` DSL).
- Provider portability = single DI swap, zero application code changes.

---

## P0: MEAI Embedding Generation — Platform Alignment (Done)

### Current State

`EmbeddingService` wraps `IEmbeddingGenerator<string, Embedding<float>>` (MEAI) with:
- 8000-character text truncation
- `RssItem`-to-text composition (`$"{item.Title}\n{item.Summary}"`)
- `ConfigureAwait(false)` on all async calls

### What to Extract / Improve

- Make truncation a configurable option, not a hard-coded constant.
- Add retry / fallback: if primary embedding model fails, try a secondary.
- Register as DI service: `services.AddSingleton<IEmbeddingGenerator>(sp => ...)`.
- Consider `EmbeddingGeneratorBuilder` for pipeline composition (MEAI middleware pattern).

### API Surface (Proposed)

```csharp
// No new interface needed — use IEmbeddingGenerator directly.
// Add configuration via options pattern:
public sealed record EmbeddingOptions
{
    public int MaxChars { get; init; } = 8000;
    public string? FallbackModel { get; init; }
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

```csharp
public sealed record ClassificationResult
{
    public string Signal { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public bool IsNoise { get; init; }
    public string? HallucinatedSignal { get; init; }
}

public sealed record ClassificationDecision
{
    public ClassificationResult Result { get; init; } = new();
    public string Source { get; init; } = string.Empty;
    public NeighborStats? Stats { get; init; }
    public bool WasAutoLabelled { get; init; }
}
```

### What to Extract

- Move to `WorkplaceIQ.AI.Models` as shared types.
- Convert `ClassificationSources` constants to `enum ClassificationSource`.
- Add EF Core value converter for the enum to string column.
- Add JSON serialization attributes for SignalR / API use.

---

## P1: `VectorClassifier` — Multi-Strategy Hybrid Classifier

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/VectorClassifier.cs`

### Current Form

A concrete class implementing a 3-tier hybrid classification strategy on top of MEVD `VectorStore`:

1. **Bootstrap** (< 20 classified items): Direct LLM fallback. No vector search.
2. **Vector Auto** (≥ threshold, high confidence): kNN search via `collection.SearchAsync()` → confidence gates (min neighbors, min agreement, min similarity, min margin) → centroid agreement → `VectorAuto`.
3. **LLM Fallback** (sparse neighbors or low confidence): Falls back to `IChatClient` via `LlmFallbackDelegate`.

Gates: `topK=10`, `minNeighbors=5`, `minNeighborAgreement=5`, `minAvgSimilarity=0.86`, `minMargin=0.10`.

### What to Extract

- Refactor into strategy pattern: `IClassificationStrategy` with `VectorClassificationStrategy`, `LlmClassificationStrategy`, `HybridClassificationStrategy`.
- Extract gate parameters into `ClassificationOptions` record.
- Inject via DI: `services.AddHybridClassifier(options => ...)`.

### API Surface (Proposed)

```csharp
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
`src/WorkplaceIQ.Web/SignalFlow/Services/CategoryCentroidTracker.cs`

### Current Form

Thread-safe, incremental running-mean centroid tracker per category using Welford's online algorithm:
- `AddOrUpdate(signal, embedding)` — O(1)
- `GetCentroidSimilarity(signal, embedding)` — cosine via `TensorPrimitives.CosineSimilarity`
- `GetBestCentroidMatch(embedding, minSimilarity)` — best match across all centroids
- `ConcurrentDictionary` with per-key locking

### What to Extract

- `ICentroidTracker` interface with `InMemoryCentroidTracker` implementation.
- Add `RedisCentroidTracker` for distributed scenarios.
- Add centroid persistence (currently rebuilt from `ClassifiedItem` table on startup).
- Register as singleton: `services.AddSingleton<ICentroidTracker, InMemoryCentroidTracker>()`.

---

## P1: `IFeedbackService` — Human-in-the-Loop Pattern

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/IFeedbackService.cs` + `FeedbackService.cs`

### Current Form

A service with both read queries (dashboard data) and write operations (feedback actions). `MoreLikeAsync` delegates to MEVD `VectorStore.SearchAsync()` — O(1) memory, not O(n).

### What to Extract

- Split into `IClassificationStore` (reads) and `IClassificationFeedback` (writes).
- Extract `RetryFailedAsync` logic into the job infrastructure.
- Make `MoreLikeAsync` a reusable `ISimilaritySearch` service.

---

## P1: `EmbeddingSerializer` — Vector Serialization Utility

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/EmbeddingSerializer.cs`

### Current Form

```csharp
public static byte[] ToBytes(ReadOnlyMemory<float> embedding)
    => MemoryMarshal.AsBytes(embedding.Span).ToArray();

public static ReadOnlyMemory<float> FromBytes(byte[] bytes)
    => MemoryMarshal.Cast<byte, float>(bytes).ToArray().AsMemory();
```

### What to Extract

- Move to `WorkplaceIQ.AI.Utilities`.
- Add null/empty handling.
- Consider `Embedding<T>` struct that wraps `ReadOnlyMemory<float>` and handles serialization internally.

---

## P2: `PipelineBackgroundService` — Background Job Infrastructure

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/PipelineBackgroundService.cs`

### Current Form

A `BackgroundService` that:
- Accepts requests via `Channel<PipelineRequest>` (bounded, single-reader, drop-write on overflow)
- Creates DI scope per pipeline run
- Tracks `PipelineState(IsRunning, LastEvent?)` for reconnection recovery
- Uses `PipelineProgressReporter` to bridge `IProgress<PipelineEvent>` → SignalR

### What to Extract

1. **`JobQueue<TJob>`** — Channel-based queue with backpressure.
2. **`JobRunner<TJob, TEvent>`** — `BackgroundService` base class:
   - Reads from queue, creates DI scopes, manages cancellation, tracks state.
3. **`ProgressReporter<TEvent>`** — Generic `IProgress<T>` → SignalR adapter.
4. **Job event types** — Generic discriminated union:
   - `JobStarted<TStage>`, `JobProgress<TStage>`, `JobItemProcessed<TItem>`, `JobFailed<TStage>`, `JobCompleted`

### API Surface (Proposed)

```csharp
public interface IJobQueue<TJob>
{
    bool TryEnqueue(TJob job);
    JobState GetState();
}

public abstract class JobRunner<TJob, TEvent> : BackgroundService
{
    protected abstract IJobQueue<TJob> Queue { get; }
    protected abstract Task RunAsync(TJob job, IProgress<TEvent> progress, CancellationToken ct);
}
```

---

## P2: Pipeline Event Types

### Source
`src/WorkplaceIQ.Web/SignalFlow/Models/PipelineEvent.cs`

### Current Form

```
PipelineEvent (base, with [JsonDerivedType])
├── PipelineStarted(totalFeeds)
├── PipelineProgress(stage, current, total, message?)
├── PipelineItemProcessed(id, title, signal, isNoise, reasoning, hallucinatedSignal?)
├── PipelineFailed(stage, error, contentId?)
└── PipelineCompleted(totalItems, signalCount, noiseCount, failedCount, signalBreakdown?)
```

### What to Extract

- Generic job event types in `WorkplaceIQ.Jobs.Events`.
- Keep `[JsonDerivedType]` for SignalR compatibility.

---

## P2: `RssFetcher` — Content Connector Pattern

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/RssFetcher.cs`

### What to Extract

- `IContentConnector<TItem>` interface with `RssConnector` implementation.
- Use `IHttpClientFactory` instead of raw `HttpClient`.
- Content hash / fingerprint as `IContentHasher` with SHA256.
- `IConnectorRegistry` to register/enumerate connectors.

---

## P2: `ConfigLoader` — Feature Configuration Pattern

### Source
`src/WorkplaceIQ.Web/SignalFlow/Services/ConfigLoader.cs`

### What to Extract

- `IFeatureConfiguration<TConfig>` interface.
- Backends: `FileConfigProvider`, `JsonConfigProvider`, `DbConfigProvider`.
- Merge/override: env vars > files > defaults.
- Schema validation via `System.ComponentModel.DataAnnotations`.

---

## P3: Future Platform Pillars

### MEDI — `Microsoft.Extensions.DataIngestion`

When the chunking/embedding pipeline needs formal structure (beyond the current ad-hoc feed → embed → store cycle), MEDI provides:
- Document readers (PDF, HTML, Markdown)
- Chunking strategies (token-based, section-based, sliding window)
- Enrichment transformations (summarization, keyword extraction)
- Built on MEAI + MEVD

### System.Numerics.Tensors

`Tensor<T>` (experimental in .NET 9) provides:
- Multi-dimensional tensor operations
- Zero-copy interop with ML.NET, TorchSharp, ONNX Runtime
- Efficient data manipulation with indexing and slicing

Already using `TensorPrimitives.CosineSimilarity` — worth monitoring `Tensor<T>` as it matures.

### MCP — Model Context Protocol

When SignalFlow capabilities need to cross process or product boundaries, expose them as MCP servers.

---

## Summary: What to Extract and In What Order

| Phase | What | Why Now |
|---|---|---|
| **1** | `ClassificationSource` enum | Required by any AI feature; trivial |
| **1** | Core classification records to `WorkplaceIQ.AI.Models` | Required by any AI feature; zero deps |
| **1** | `EmbeddingSerializer` to `WorkplaceIQ.AI.Utilities` | Required by any vector feature |
| **2** | `ICentroidTracker` + `InMemoryCentroidTracker` | Needed for classification quality |
| **2** | MEVD provider swap (pgvector) | Production vector store |
| **2** | MEAI embedding config (truncation, retry) | Production hardening |
| **3** | `VectorClassifier` as strategy pattern | Flagship AI feature; proves abstractions |
| **3** | `IFeedbackService` split + extract | Production feedback loop |
| **4** | Job infrastructure (`JobQueue`, `JobRunner`) | Background processing for any pipeline |
| **5** | Connector patterns (`IContentConnector`) | Reusable content ingestion |
| **5** | Configuration pattern (`IFeatureConfiguration`) | Replaces ad-hoc parsing |

> **Bottom line:** SignalFlow contains ~3,000 lines of production-tested code built directly on MEAI + MEVD. Extractions add configuration, provider portability, and shared models — not new abstractions. The P0 and P1 items form the foundation for every AI feature the platform will support.
