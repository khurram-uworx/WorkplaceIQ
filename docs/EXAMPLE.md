# SignalFlow Example

> A complete implementation of the Streamix AIDataEngg signal intelligence pipeline running on WorkplaceIQ primitives — demonstrating the platform's UI story while exercising LLM, vector search, and real-time pipeline orchestration.

---

## Rationale

The [Streamix AIDataEngg](https://github.com/khurram-uworx/Streamix/tree/main/examples/AIDataEngg) example is a modern content intelligence pipeline: fetch RSS feeds, classify them into business signals using a hybrid vector+LLM classifier, persist results, and provide a feedback loop for refinement.

We are reimplementing it on WorkplaceIQ to:

1. **Prove the UI story** — WorkplaceIQ's tag helpers, content model, labels, metrics, and layout primitives can render an experience equivalent to the original ASP.NET host, even without the AI features yet landed in the platform.
2. **Surface missing abstractions** — Building with real LLM + vector store concretely shows where `IVectorStore`, `IEmbeddingService`, pipeline base classes, and store extensions need to live in the library layers.
3. **Create a living spec** — Once working, the example becomes the reference for what WorkplaceIQ's "Coming Up" AI features (vector search, AI chat, insights) should look like when they land in the platform.

---

## Scope

| Feature | Included | Notes |
|---|---|---|
| RSS feed fetching | Yes | `System.ServiceModel.Syndication`, parallel |
| Dedup + persistence | Yes | Content hash, stored as Content records |
| Embedding generation | Yes | Via `IEmbeddingGenerator<string, Embedding<float>>` |
| Hybrid vector + LLM classification | Yes | InMemory vector store, kNN bootstrap → LLM fallback |
| Results storage | Yes | New `ClassifiedItem` entity + `ContentLabel` for signal tag |
| Interactive feedback (reclassify, mark-not-noise, delete, more-like) | Yes | SignalR-based, webmail-style UI |
| Real-time pipeline progress | Yes | SignalR hub, workflow chart, log |
| Configuration editor | **No** | Config files (`source.md`, `goal.md`, `signals.md`, `prompt.md`, `engine.md`) are static files copied to output |
| Multi-tenancy / auth | No | Single-operator demo |
| pgvector | No | InMemory only; abstraction designed to swap later |

---

## Architecture

### Project

The example lives **inside `WorkplaceIQ.Web`** — no new projects. The feature is organized under a `SignalFlow/` folder within the web project, keeping it visually separate from the existing demo app.

```
WorkplaceIQ.Web/
├── SignalFlow/
│   ├── Controllers/
│   │   └── SignalFlowController.cs
│   ├── Hubs/
│   │   └── PipelineHub.cs
│   ├── Services/
│   │   ├── PipelineBackgroundService.cs
│   │   ├── RssFetcher.cs
│   │   ├── IVectorStore.cs
│   │   ├── InMemoryVectorStore.cs
│   │   ├── EmbeddingService.cs
│   │   ├── EmbeddingSerializer.cs
│   │   ├── CategoryCentroidTracker.cs
│   │   ├── VectorClassifier.cs
│   │   ├── PipelineEvent.cs
│   │   ├── IFeedbackService.cs
│   │   └── FeedbackService.cs
│   ├── Models/
│   │   ├── ClassifiedItem.cs
│   │   └── SignalGroup.cs
│   ├── Views/
│   │   ├── _SignalFlowLayout.cshtml
│   │   ├── Dashboard.cshtml
│   │   ├── Signals.cshtml
│   │   ├── Signal.cshtml
│   │   ├── Item.cshtml
│   │   ├── Noise.cshtml
│   │   └── Bounced.cshtml
│   └── configs/
│       ├── source.md
│       ├── goal.md
│       ├── signals.md
│       ├── prompt.md
│       └── engine.md
├── wwwroot/
│   ├── css/signalflow.css
│   └── js/signalflow.js
```

### NuGet Dependencies (added to `WorkplaceIQ.Web.csproj`)

| Package | Purpose |
|---|---|
| `Streamix` 1.2.2 | Pipeline orchestration (`Flux.ScopedAsync`, `FlatMap`, `Retry`, `Checkpoint`, `DrainAsync`) |
| `Streamix.AspNetCore` 1.2.2 | ASP.NET integration helpers |
| `Microsoft.Extensions.AI` | `IChatClient`, `IEmbeddingGenerator` abstractions |
| `Microsoft.Extensions.AI.OpenAI` | OpenAI-compatible client (Ollama, Azure OpenAI, Foundry) |
| `System.ServiceModel.Syndication` | RSS/Atom feed parsing |
| `Microsoft.SemanticKernel.Connectors.InMemory` | In-memory vector store (dev) |
| `System.Numerics.Tensors` | `TensorPrimitives` for centroid math |

> **Note on `EfFlux`**: The original Streamix example uses `EfFlux.FromStreamed` from `Streamix.Extensions` to stream EF queries as `IFlux<T>`. That project is available as NuGet `Streamix.Extensions` 1.2.2-alpha, already referenced.

### Data Model

**Existing primitives used:**

| WorkplaceIQ Primitive | SignalFlow Role |
|---|---|
| `Content` (ContentType = `"SignalFlowItem"`) | RSS item (title, body=summary, MetadataJson for link/feed/hash) |
| `FeedContainer` | Logical parent container for all SignalFlow items |
| `Label` | Signal names (AI/ML, Security, General, etc.) |
| `ContentLabel` | Links an RSS item to its signal label |
| `ContentRelationship` (type = `"similar"`) | "More like this" — linked similar items |
| `MetricDefinition` / `<iq-metric>` | Dashboard stats |

**New entity — `ClassifiedItem`** (in `WorkplaceIQ.Content` namespace):

```
ClassifiedItem
├── Id: Guid
├── ContentId: Guid (FK → Content)
├── RssItem: Content (nav)
├── LabelId: Guid (FK → Label)
├── SignalLabel: Label (nav)
├── Reasoning: string?
├── IsNoise: bool
├── AttemptCount: int
├── HallucinatedSignal: string?
├── Embedding: byte[]? (serialized float[])
├── ClassificationSource: string   — see values below
└── ClassifiedAt: DateTimeOffset
```

`ClassificationSource` uses string constants matching the original enum:

| Value | Meaning |
|---|---|
| `"Bootstrap"` | LLM fallback before vector bootstrap threshold |
| `"VectorAuto"` | Vector kNN passed all confidence gates |
| `"LlmSparseNeighbors"` | Too few neighbors found |
| `"LlmLowConfidence"` | Neighbors found but confidence gates failed |
| `"LlmEmbeddingFailed"` | Embedding generation failed, fell back to LLM |
| `"Failed"` | All retries exhausted, last was hallucinated signal |

This entity is added directly to `WorkplaceIqDbContext` as a first-class table — a deliberate enrichment of the platform data model.

**Supporting models** (in `SignalFlow/Models/` or `WorkplaceIQ.Content`):

| Type | Fields | Purpose |
|---|---|---|
| `ClassificationResult` | `Signal`, `Reasoning`, `IsNoise`, `HallucinatedSignal?` | Structured LLM output |
| `ClassificationDecision` | `Result` (ClassificationResult), `Source` (string), `Stats` (NeighborStats), `WasAutoLabelled` | Pipeline classification outcome |
| `NeighborStats` | `NeighborCount`, `TopSignal`, `TopSignalAgreement`, `AverageSimilarity`, `Margin` | Vector search statistics for gating |

### Pipeline Flow

```
Config (env vars + config files)
  ↓
PipelineBackgroundService.ExecuteAsync
  ├── Stage 0: RestoreVectorStateAsync
  │   → load prior embeddings from ClassifiedItem table
  │   → rebuild in-memory vector store + centroids
  │
  ├── Stage 1-2: Flux.ScopedAsync
  │   → Flux.From(feedSources)
  │     .FlatMap(url => RssFetcher.FetchFeedAsync(url, name), 4)
  │     .FlatMap(item => dedup + save as Content, 4)
  │     .DrainAsync()
  │
  ├── Stage 3: Flux.From(GetUnprocessedItemsAsync)
  │   → raw EF AsAsyncEnumerable() wrapping
  │   → count + report progress
  │
  ├── Stage 4: FlatMap(embedding generation, 4 concurrency)
  │
  └── Stage 5: ForEachAsync(hybrid classify, 1 concurrency)
                   → VectorClassifier.ClassifyAsync
                     └─ if totalClassifiedCount < bootstrapThreshold
                          → Bootstrap (LLM)
                        └─ else
                          → vector kNN search → confidence gates
                            └─ passes → VectorAuto
                            └─ fails  → LLM fallback
                   → PersistAndIndexAsync (ClassifiedItem + ContentLabel + vector store)

SignalR ← PipelineEvent (polymorphic, JsonDerivedType serialization for SignalR)
```

**PipelineEvent hierarchy** (all share `Type` discriminator + `Timestamp`):

| Record | Fields | When |
|---|---|---|
| `PipelineStarted` | `TotalFeeds` | Pipeline begins |
| `PipelineProgress` | `Stage`, `Current`, `Total`, `Message?` | Stage advancement |
| `PipelineItemProcessed` | `RssItemId`, `Title`, `Signal`, `IsNoise`, `Reasoning`, `HallucinatedSignal?` | Per-item result |
| `PipelineFailed` | `Stage`, `Error`, `RssItemId?` | Stage or item error |
| `PipelineCompleted` | `TotalItems`, `SignalCount`, `NoiseCount`, `FailedCount`, `SignalBreakdown?` | Pipeline done |

**Pipeline trigger flow**: UI "Process" button → SignalR `connection.invoke("TriggerPipeline")` → `PipelineHub.TriggerPipeline()` reads config → `PipelineBackgroundService.EnqueueAsync()` → background loop runs pipeline → events broadcast via `PipelineProgressReporter` → JS `onPipelineEvent()` updates dashboard + log.

### UI Pages

| Route | Page | Description |
|---|---|---|
| `/signalflow` | Dashboard | Stats grid + workflow progress chart + recent classifications |
| `/signalflow/signals` | Signals | Collapsible signal groups with item lists |
| `/signalflow/signals/{signal}` | Signal | Items for one signal |
| `/signalflow/item/{id}` | Item | Detail + reclassify / more-like / delete / mark-not-noise |
| `/signalflow/noise` | Noise | Items classified as irrelevant |
| `/signalflow/bounced` | Bounced | Items that exhausted retries |

Layout: webmail-style left sidebar with signal folders + counts, a prominent "Process" button in the sidebar footer that triggers the pipeline, and nav links to Dashboard / Signals / Noise / Bounced.

**Main layout nav**: A "Signal Intelligence" link is added to the existing `_Layout.cshtml` navbar alongside Dashboard / News / Incidents / Discussions / Documents. Clicking it navigates to `/signalflow` which uses the SignalFlow-specific layout.

---

## Implementation Tasks

### Phase 1: Data Model & Store Extensions

- [ ] Add `ClassifiedItem` entity to `WorkplaceIQ.Content`
- [ ] Add `ClassifiedItems` DbSet + fluent config to `WorkplaceIqDbContext`
- [ ] Extend `IWorkplaceIqStore` with classification CRUD queries
- [ ] Implement in `EfWorkplaceIqStore`

### Phase 1.5: Supporting Models

- [ ] `ClassificationResult` record — `Signal`, `Reasoning`, `IsNoise`, `HallucinatedSignal?`
- [ ] `ClassificationDecision` record — `Result`, `Source`, `Stats`, `WasAutoLabelled`
- [ ] `NeighborStats` record — `NeighborCount`, `TopSignal`, `TopSignalAgreement`, `AverageSimilarity`, `Margin` + factory `From(hits)` + `Empty`
- [ ] `SignalGroup` record — `Signal`, `Count`, `Items`

### Phase 2: Pipeline Services

- [ ] `RssFetcher` — `FetchFeedAsync(url, name)` → `IAsyncEnumerable<RssItem>` (uses `SyndicationFeed.Load`, SHA256 content hash)
- [ ] `IVectorStore` + `InMemoryVectorStore` — `SearchAsync`, `UpsertAsync`, `GetAsync` (wraps `SemanticKernel.Connectors.InMemory`)
- [ ] `VectorIndexEntry` — `Id`, `RssItemId`, `Signal`, `Title`, `Summary`, `Embedding` (`[VectorStoreKey]`, `[VectorStoreData]` attributes)
- [ ] `EmbeddingSerializer` — `ToBytes` / `FromBytes` round-trip
- [ ] `CategoryCentroidTracker` — thread-safe running-mean per signal (uses `TensorPrimitives` for cosine similarity)
- [ ] `EmbeddingService` — wraps `IEmbeddingGenerator<string, Embedding<float>>`, 8000-char truncation, generates from title+summary
- [ ] `VectorClassifier` — hybrid kNN + LLM fallback with confidence gating (bootstrapThreshold=20, topK=10, minNeighbors=5, minNeighborAgreement=5, minAvgSimilarity=0.86, minMargin=0.10)
- [ ] `PipelineEvent` records — `PipelineStarted`, `PipelineProgress`, `PipelineItemProcessed`, `PipelineFailed`, `PipelineCompleted` (polymorphic with `[JsonDerivedType]`)
- [ ] `RssItem` record (internal) — `FeedUrl`, `FeedName`, `Title`, `Summary`, `Link`, `Published`, `ContentHash` (used during fetch, then mapped to `Content`)

### Phase 3: Pipeline Orchestration

- [ ] `PipelineBackgroundService` — Streamix pipeline with `Flux.ScopedAsync`
- [ ] `IFeedbackService` + `FeedbackService` — reclassify, more-like, mark-not-noise, delete

### Phase 4: Web UI

- [ ] `PipelineHub` — SignalR hub
- [ ] `_SignalFlowLayout.cshtml` — sidebar + main
- [ ] `SignalFlowController` — all page actions
- [ ] Dashboard view — stats + workflow + recent
- [ ] Signals view — signal groups
- [ ] Signal view — items per signal
- [ ] Item view — detail + actions
- [ ] Noise / Bounced views
- [ ] `signalflow.css` — layout + component styles
- [ ] `signalflow.js` — SignalR client + UI updates

### Phase 5: Integration

- [ ] Copy `configs/*.md` from Streamix example
- [ ] Add AI config to `appsettings.json` / env vars
- [ ] DI registration in `Program.cs`
- [ ] NuGet packages in `.csproj`
- [ ] Update `DemoDataSeeder` — signal labels
- [ ] Build + verify

---

## Notes for Implementation

- **Config files** are read directly from `SignalFlow/configs/` directory via `File.ReadAllText` — no `ConfigLoader` abstraction needed. Engine settings (endpoint, model, dimension, classifier thresholds) read at startup from `engine.md`.
- **`EfFlux` available** — `Streamix.Extensions` 1.2.2-alpha is on NuGet (already referenced). Use `EfFlux.FromStreamed(...)` for reading unprocessed items.
- **Pipeline startup restore** — On each run, `RestoreVectorStateAsync` loads prior embeddings from `ClassifiedItem` table into the in-memory vector store and centroids. This ensures the vector classifier benefits from all prior classifications, even though the store itself is volatile.
- **AI not available?** The pipeline gracefully handles missing endpoints. Set `AI_ENDPOINT=http://localhost:11434/v1` for local Ollama. The app won't crash on missing config — pipeline stages emit errors as `PipelineEvent` failures.
- **Cancellation** — pipeline supports `CancellationToken` from `BackgroundService.StopAsync`. A "Cancel" button can be added later.
- **Streamix** is used for pipeline orchestration, not for UI data flow. The existing MVC + SignalR patterns handle all UI concerns.
- **MoreLikeAsync** — Runs `TensorPrimitives.CosineSimilarity` in C# over all stored embeddings from the `ClassifiedItem` table. Adequate for dev-scale (<10K items). For production, replace with vector store `SearchAsync`.
- **RetryFailedAsync** — Sets the item's `Content.Status` back to `"active"` (or similar "unprocessed" state) and resets `AttemptCount = 0`, so the next pipeline run re-classifies it.
- **SignalR serialization** — `PipelineEvent` records use `[JsonDerivedType]` for polymorphic serialization. The JS client dispatches on `event.type` ("started", "progress", "itemProcessed", "failed", "completed").

---

## Maturation Roadmap

Post-implementation, each of these is a candidate for extraction into the library:

| Discovery Trigger | Target | Action |
|---|---|---|
| `ClassifiedItem` entity | `WorkplaceIQ` core | Already done (landed in core) |
| `ClassificationResult` / `ClassificationDecision` / `NeighborStats` | `WorkplaceIQ` core | Extract common AI pipeline types |
| `IVectorStore` + `VectorIndexEntry` | `WorkplaceIQ` core | Extract from example |
| `IEmbeddingService` | `WorkplaceIQ` core | Extract from example |
| Vector-aware store queries (by label, unprocessed count) | `IWorkplaceIqStore` | Add methods |
| `RssFetcher` as reusable service | `WorkplaceIQ.AspNet` | Extract (or keep as example-specific) |
| Pipeline BackgroundService base class | `WorkplaceIQ.AspNet` | Extract `PipelineBackgroundService` pattern |
| SignalR progress hub base class | `WorkplaceIQ.AspNet` | Extract `PipelineHub` + `PipelineEvent` pattern |
| Label-as-signal (metadata on labels) | `WorkplaceIQ` core | Add signal color/description fields to `Label` |
| `pgvector` implementation | `WorkplaceIQ.AspNet` | Add as `IVectorStore` provider |
| `EfFlux` replacement (streaming EF queries) | `WorkplaceIQ.AspNet` | Extract `EfFlux.FromStreamed`-equivalent |
