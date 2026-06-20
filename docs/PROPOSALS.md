# Architecture Decisions

> Design decisions made while evolving WorkplaceIQ's AI infrastructure. Each entry links to [OBSERVATIONS.md](./OBSERVATIONS.md) and [POTENTIAL-AI-FEATURES.md](./POTENTIAL-AI-FEATURES.md).

---

## ADR-1: Replace Custom `IVectorStore` with MEVD `VectorStore`

**Status:** Completed &nbsp;|&nbsp; **Date:** 2026-06 &nbsp;|&nbsp; **Drivers:** OBSERVATIONS §1, §5, §10 &nbsp;|&nbsp; **Supports:** POTENTIAL-AI-FEATURES P0, P1

### Context

SignalFlow originally had a custom `ISignalFlowVectorStore` interface with hand-rolled in-memory implementation:

```csharp
// Before: custom interface, O(n) memory for MoreLikeAsync, no filtering
Task<IAsyncEnumerable<(VectorIndexEntry Record, float Score)>> SearchAsync(
    ReadOnlyMemory<float> embedding, int topK, CancellationToken ct);
```

Two problems:
1. **`MoreLikeAsync`** loaded every `ClassifiedItem` embedding into memory and computed cosine similarity in C# (O(n) memory/CPU).
2. **No metadata filtering** — searches always spanned all entries including noise. No way to filter by signal, date, or exclude noise.

### Decision

Replace `ISignalFlowVectorStore` + `SignalFlowInMemoryVectorStore` + `VectorIndexEntry` with Microsoft's official `Microsoft.Extensions.VectorData` (MEVD) abstraction — the .NET platform standard for vector storage and retrieval, analogous to `Microsoft.Extensions.AI` for chat and embeddings.

### Design

**SignalFlowVectorEntry** — MEVD record class with attributes:

```csharp
[VectorStoreKey]
public string Id { get; set; } = string.Empty;

[VectorStoreData]
public string Signal { get; set; } = string.Empty;

[VectorStoreData]
public bool IsNoise { get; set; }

[VectorStoreData]
public DateTimeOffset ClassifiedAt { get; set; }

[VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineDistance)]
public ReadOnlyMemory<float>? Embedding { get; set; }
```

**SignalFlowVectorSchema** — runtime schema factory (like CodeMemory's `VectorSchema`):

```csharp
public static VectorStoreCollectionDefinition CreateEntryDefinition(int dimension)
```

The embed dimension comes from `configs/engine.md → PipelineConfig.EmbeddingDimension` (default 768, matches `nomic-embed-text`). Override matches the model.

**Collection injection** — services receive `VectorStoreCollection<string, SignalFlowVectorEntry>` directly, built once at startup with the correct dimension:

```csharp
// Program.cs
var collection = vectorStore.GetCollection<string, SignalFlowVectorEntry>(
    SignalFlowVectorEntry.CollectionName,
    SignalFlowVectorSchema.CreateEntryDefinition(sigConfig.EmbeddingDimension));
builder.Services.AddSingleton(collection);
```

**Filtered search** — MEVD's LINQ-based `VectorSearchOptions<TRecord>.Filter`:

```csharp
var options = new VectorSearchOptions<SignalFlowVectorEntry>
{
    Filter = e => !e.IsNoise
};
```

**Provider dispatch** — `CreateVectorStore(provider)` switches on `Storage:Provider` (matching EF Core dispatch). Currently `inmemory` is wired; `pgvector`/`sqlite`/`sqlserver` throw with guidance to add the appropriate `Microsoft.SemanticKernel.Connectors.*` package.

### Changes

| File | Change |
|---|---|
| `SignalFlowVectorEntry.cs` | New — MEVD record with `[VectorStoreKey]`, `[VectorStoreData]`, `[VectorStoreVector]` |
| `SignalFlowVectorSchema.cs` | New — programmatic collection definition factory |
| `VectorClassifier.cs` | Injects `VectorStoreCollection`; uses `Filter = e => !e.IsNoise` |
| `FeedbackService.cs` | `MoreLikeAsync` delegates to `SearchAsync` — O(1) memory; injects collection directly |
| `PipelineOrchestrator.cs` | Injects collection directly; DB-based count instead of vector-store count |
| `Program.cs` | Collection built with schema; `CreateVectorStore` provider dispatch |
| `ISignalFlowVectorStore.cs` | Deleted |
| `InMemoryVectorStore.cs` | Deleted |
| `VectorIndexEntry.cs` | Deleted |

### Outcome

| Concern | Before | After |
|---|---|---|
| Vector store interface | Custom `ISignalFlowVectorStore` | MEVD `VectorStore` — platform abstraction |
| Metadata filtering | None | `VectorSearchOptions<TRecord>.Filter` (LINQ) |
| `MoreLikeAsync` | O(n) memory, O(n) CPU | `SearchAsync` — O(1) memory |
| Provider swap | Rewrite interface + impl | DI registration change |
| Vector dimension | Hard-coded 1536 | Config-driven from `engine.md` |
| Thread safety | Manual `lock(gate)` | MEVD implementations thread-safe |
| Collection name | Duplicated across 3 files | Centralized on `SignalFlowVectorEntry.CollectionName` |

### What stayed the same

- `VectorClassifier` classification logic (bootstrap gates, confidence gates, centroid check)
- `PipelineOrchestrator` pipeline orchestration flow
- `CategoryCentroidTracker` — still updated separately
- `EmbeddingService` — uses MEAI `IEmbeddingGenerator`
- `ClassifiedItem` DB entity — still the canonical persistence store via EF Core

### Unresolved

- **`ClassifiedItem.Embedding byte[]`** column — can be dropped once pgvector is in production, making the vector store the sole embedding source.
