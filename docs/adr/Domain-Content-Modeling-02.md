# ADR-Domain-Content-Modeling-02: Unified Polymorphic Content Model

---

## Context

WorkplaceIQ is a metadata-driven content platform. Every domain entity — Discussions, Files, Feeds, and their child items (posts, file records, feed entries) — shares common infrastructure needs:

- **Labels** — tag-based categorization, cross-cutting across all entity types
- **Metadata** — key-value pairs for extensible attributes per entity
- **Metrics** — view counts, activity timestamps, engagement data
- **Vector indexing** — semantic search and SignalFlow classification

Without a unified model, every new entity type requires duplicating the label/metadata/metric infrastructure, leading to schema proliferation and inconsistent query patterns. A polymorphic content model centralizes these concerns under a single abstraction.

The target audience is small-to-mid enterprises (2–50 people) where Sharepoint / M365 do not yet make economic sense. Content volumes are modest; query performance considerations favor simplicity over denormalization.

---

## Decision

**Adopt a unified polymorphic content model** with two abstract levels (`Content` and `ContentItem`), a concrete `Container` abstraction for grouping and infrastructure ownership, and per-type friendly integer identifiers alongside canonical GUIDs.

### Entity Hierarchy

```
Content                              (GUID PK, Labels, Metadata, Metrics)
├── Container : Content              (per-type int BusinessId, VectorStore ref, permission scope, children)
│   ├── Discussion
│   ├── FileFolder
│   └── Feed
└── ContentItem : Content            (GUID FK → parent Container)
    ├── TopicPost
    ├── FileRecord
    └── FeedEntry
```

### 1. Content (Abstract Base)

Every entity in the system is a `Content`. The `Contents` table holds:

| Column | Type | Purpose |
|---|---|---|
| `Id` | GUID PK | Universal identifier, no collisions, merge-safe |
| `Discriminator` | string | Identifies the concrete type (Discussion, TopicPost, etc.) |
| `CreatedAt`, `CreatedBy` | ... | Audit trail |
| `ModifiedAt`, `ModifiedBy` | ... | Audit trail |

All `Contents` rows may have associated labels, metadata, and metrics via separate link tables (`ContentLabels`, `ContentMetadata`, `ContentMetrics`). This avoids table-width bloat on the core `Contents` table.

### 2. Container (Concrete Subclass of Content)

`Container` represents entities that own children, manage a vector collection, and define a permission boundary.

| Column | Type | Purpose |
|---|---|---|
| `Id` | GUID PK (inherited from Content) | Canonical identity |
| `BusinessId` | int (nullable) | Per-type sequential friendly ID for URLs (`/Discussions/View/1`) |
| `VectorCollectionName` | string (nullable) | Name of the vector store collection for semantic search |
| `Name` | string | Human-readable label |
| `Description` | string | Optional description |

The `BusinessId` is scoped per concrete type — `Discussion` IDs 1, 2, 3 and `FileFolder` IDs 1, 2, 3 coexist as independent sequences. This gives clean URLs without global sequence coupling. The `BusinessId` is a friendly identifier, not an authoritative FK — move/merge operations treat it as a stable label that can be reassigned.

Vector indexing operates at the Container level. All ContentItems within a Container are indexed into the same collection.

### 3. ContentItem (Abstract Subclass of Content)

`ContentItem` represents entities that belong to a parent Container. A single `ContentItems` table serves all item types.

| Column | Type | Purpose |
|---|---|---|
| `Id` | GUID PK (inherited from Content) | Canonical identity |
| `ContainerId` | GUID FK → Content.Id | Parent Container |
| `Discriminator` | string | TopicPost, FileRecord, FeedEntry |
| `BusinessId` | int (nullable) | Per-container sequential friendly ID for URLs |
| `ContentData` | jsonb (optional) | Flexible payload for type-specific data |

If a business entity needs additional structured columns (e.g., `FileRecord` with a file hash and storage path), it creates an optional child table with `Id = GUID FK → ContentItems.Id`. No polymorphism in the schema — just identity sharing.

The `BusinessId` is scoped per parent Container. Moving an item between containers copies the literal content (new GUID, new `BusinessId` under target container) and deletes the old row, all within a single transaction. This avoids ID reassignment complexity and gives move-as-copy-and-delete semantics that can later be extended to versioning or history.

### 4. Labels, Metadata, Metrics (Infrastructure)

These are split per entity level to keep the hot `Contents` table lean:

- `ContentLabels`, `ContentMetadata`, `ContentMetrics` — for `Content`-level entities (including Containers)
- `ContentItemLabels`, `ContentItemMetadata`, `ContentItemMetrics` — for `ContentItem`-level entities

The schema for each is identical; the split is purely to avoid index pressure from item-level data against container-level queries and vice versa.

### 5. Orphan Cleanup

A GUID row in `Contents` with no matching concrete-typed row (`Discussions`, `FileFolders`, etc.) is a data anomaly — it exists but is invisible in the business layer. This is rare and happens only from partial failures or bugs. A background job scans `Contents` by discriminator, checks for the existence of the corresponding type table row, and deletes orphans. Tolerated as an acceptable inconsistency given the target scale.

---

## Rationale

### 1. Uniform extensibility

Adding a new entity type (e.g., `EventCalendar`) requires only:
- A new discriminator value
- One optional type-specific table if extra columns are needed
- Labels, metadata, metrics, and vector indexing work automatically

No new infrastructure tables, no new query patterns for cross-cutting concerns.

### 2. GUID + Integer dual identity

GUIDs serve as the universal identity across the system — merge-safe, no rekeying, no collisions. Per-type integer sequences give clean, short, human-friendly URLs (`/Discussions/View/1`). The two never conflict because they serve different purposes: GUIDs are the canonical FK target; integers are presentation-friendly stable labels.

### 3. Container as infrastructure boundary

Vector store collections, permission scopes, and child management all belong to `Container`. This avoids polluting every `Content` row with nullable infrastructure columns. It also maps naturally to the domain: a Discussion owns its posts, a FileFolder owns its files, a Feed owns its entries.

### 4. Copy-and-delete move semantics

Moving a `ContentItem` between containers via copy + delete avoids the thorny problem of reassigning per-container sequential IDs. The move is atomic in a transaction, and the old row is gone. If versioning is later desired, the copy step can instead mark old rows as archived.

### 5. Scale-appropriate pragmatism

At 2–50 person deployments, none of the classic RDBMS scaling problems apply. Polymorphic joins are not a performance concern. Orphan cleanup is a safety net, not a hot path. The design optimizes for developer clarity and extensibility over denormalized query performance.

---

## Consequences

### Positive

- **Single infrastructure pattern** — Labels, metadata, metrics work identically for all entity types. Developers do not re-implement tagging per feature.
- **Container-level vector indexing** — Collection lifecycle is tied to container lifecycle. No orphaned vector collections.
- **Full-stack URL design** — GUIDs for API internals, integers for user-facing URLs. Both coexist without conflict.
- **Schema minimalism** — `Contents` + `ContentItems` as the two core tables; type-specific tables are opt-in.
- **Move without ID churn** — Copy-and-delete gives clean per-container sequences without reassignment logic.

### Negative

- **Polymorphic joins** — Queries across the full content graph (e.g., "all content with label X") require `Contents` + discriminator-aware joins. Mitigated by target scale.
- **Orphan rows** — `Contents` rows without corresponding business-entity rows are possible. Tolerated; cleaned by background job.
- **Dual ID mental model** — Developers must understand that GUIDs are canonical and integers are presentation-friendly labels, not authoritative keys.
- **Copy-and-delete ≠ true move** — If the old `BusinessId` is externally referenced (e.g., bookmarked URLs), the reference breaks. Acceptable for the target audience; mitigable via redirects later.

### Neutral

- **Container abstraction** — Adds a level to the inheritance hierarchy. Earns its keep through concrete behavior (vector store, permissions, children).
- **Separate label tables per level** — Duplicates table schema but avoids index pollution between `Content` and `ContentItem` queries.

---

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| **Flat Content table with discriminator only** (no Container, no ContentItem) | No natural place for vector store / permission ownership; every query must filter by discriminator; no type-safe distinction between top-level and child entities without application logic |
| **Single Label/Metadata/Metrics table for all entities** | Polymorphic FK (ContentId or ContentItemId) with check constraints prevents clean FK enforcement; query plans degrade due to nullable columns and OR predicates |
| **Integer-only identity (no GUIDs)** | Prevents offline ID generation; merge/collision risk if data sources are combined; GUIDs are the canonical choice for distributed identity |
| **True RDBMS inheritance (table-per-type)** | Complex query generation; EF Core TPT joins are expensive; no benefit over discriminator + opt-in child tables at this scale |
| **Move via FK update (reassign parent)** | Breaks per-container `BusinessId` sequence; requires reassignment logic and risks duplicate IDs within the target container |
| **No vector indexing on ContentItems (index Container only)** | Chosen as the starting point; individual item indexing can be added later without schema changes |
| **CQRS / event-sourced content store** | Unnecessary complexity for 2–50 person enterprises; adds eventual consistency, replay, and snapshot management overhead with no clear benefit |

---

## Related

- [ADR Library-Storage-PgVector-Connector-01](docs/adr/Library-Storage-PgVector-Connector-01.md) — Vector store backend selection that Container-level indexing will use
- `AGENTS.md` — Coding conventions for the project (primary constructors, sealed classes, DI patterns)
