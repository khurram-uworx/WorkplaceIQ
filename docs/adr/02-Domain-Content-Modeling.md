# ADR-Domain-Content-Modeling-02: Unified Polymorphic Content Model

---

## Context

WorkplaceIQ is a metadata-driven content platform. Every domain entity — Discussions, Files, Feeds, Groups, and their child items (posts, file records, feed entries, members) — shares common infrastructure needs:

- **Labels** — tag-based categorization, cross-cutting across all entity types
- **Vector indexing** — semantic search and SignalFlow classification

Without a unified model, every new entity type requires duplicating the label infrastructure, leading to schema proliferation and inconsistent query patterns. A polymorphic content model centralizes these concerns under a single abstraction.

The target audience is small-to-mid enterprises (2–50 people) where Sharepoint / M365 do not yet make economic sense. Content volumes are modest; query performance considerations favor simplicity over denormalization.

---

## Decision

**Adopt a unified polymorphic content model** with two abstract levels (`Content` and `Container`), a standalone `ContentItem` table for child entities, and GUID-only identity for all entities.

### Entity Hierarchy

```
Content (abstract)                  (GUID PK, Labels)
├── Container : Content             (VectorStore ref, permission scope, children)
│   ├── DiscussionContent
│   ├── FolderContent
│   ├── FeedContent
│   └── GroupContent                (+ GroupType)
└── [not used as base — just TPT base table for containers]

ContentItem (standalone)            (GUID PK, ContainerId FK → Content.Id, Discriminator)
├── discriminator = "topic"
├── discriminator = "feed_entry"
├── discriminator = "file"          (+ optional ContentFile child row)
└── discriminator = "member"
```

### 1. Content (Abstract Base — shared TPT base for containers only)

The `Contents` table is the TPT base for all container types. It is NOT the base for `ContentItem` — items live in a separate standalone table.

| Column | Type | Purpose |
|---|---|---|
| `Id` | GUID PK | Universal identifier, no collisions, merge-safe |
| `CreatedAt`, `CreatedBy` | ... | Audit trail |
| `ModifiedAt`, `ModifiedBy` | ... | Audit trail |

Container-level labels are tracked via the `ContentLabels` link table (ContentId FK → Content.Id).

### 2. Container (Abstract Subclass of Content)

`Container` represents entities that own children, manage a vector collection, and define a permission boundary.

| Column | Type | Purpose |
|---|---|---|
| `Id` | GUID PK (inherited from Content) | Canonical identity |
| `VectorCollectionName` | string (nullable) | Name of the vector store collection for semantic search |
| `Name` | string | Unique, URL-safe slug |
| `Title` | string | Display title |
| `Description` | string (nullable) | Optional description |
| `RendererKey` | string (nullable) | Key for selecting component renderer (e.g. `forum`, `files`) |
| `Status` | string | `active`, `archived` |
| `SettingsJson` | string (nullable) | JSON configuration blob |
| `IsSystemGenerated` | bool | Flag for auto-created system containers |

Vector indexing operates at the Container level. All ContentItems within a Container are indexed into the same collection.

### 3. ContentItem (Standalone Table — Child Items)

`ContentItem` represents entities that belong to a parent Container. It is **not** part of the `Content` TPT hierarchy — it is a standalone table with its own primary key and a `ContainerId` foreign key to `Content.Id`.

| Column | Type | Purpose |
|---|---|---|
| `Id` | GUID PK | Canonical identity (independent of Content PK sequence) |
| `ContainerId` | GUID FK → Content.Id | Parent Container |
| `Discriminator` | string | `topic`, `feed_entry`, `file`, `member` |
| `Name` | string | Short name (e.g., filename) |
| `Title` | string | Display title |
| `Body` | string (nullable) | Rich text body or description |
| `AuthorUserId` | string (nullable) | External user identifier |
| `Status` | string | `active`, `archived` |
| `PublishedAt` | DateTime (nullable) | Publication timestamp |
| `ContentData` | string (nullable) | Flexible payload for type-specific data (JSON text) |
| `CreatedAt`, `CreatedBy` | ... | Audit trail |
| `ModifiedAt`, `ModifiedBy` | ... | Audit trail |

If a business entity needs additional structured columns (e.g., `file` with storage metadata), it creates an optional 1:1 child table with `Id = GUID FK → ContentItems.Id`. Currently only `ContentFile` is defined.

Item-level labels are tracked via `ContentItemLabels` (ContentItemId FK → ContentItems.Id).

### 4. Labels (Infrastructure)

Labels are split per entity level to keep queries focused:

- `ContentLabels` — for Content-level entities (Containers only; link to `Content.Id`)
- `ContentItemLabels` — for ContentItem-level entities (link to `ContentItems.Id`)

The schema for each is the same (entity FK + LabelId FK); the split avoids index pressure from item-level labels against container-level queries and vice versa.

---

## Rationale

### 1. Uniform extensibility

Adding a new entity type requires only:
- A new container type class (if top-level) or discriminator value (if child item)
- One optional type-specific table if extra columns are needed
- Labels and vector indexing work automatically

No new infrastructure tables for cross-cutting concerns.

### 2. GUID-only identity

GUIDs serve as the universal identity across the system — merge-safe, no rekeying, no collisions. Per-type or per-container integer sequences (`BusinessId`) were considered but not implemented because no caller or query currently requires them. They can be added later if URL-friendliness becomes a priority, using the same scoped-sequence approach described in alternatives.

### 3. Container as infrastructure boundary

Vector store collections, permission scopes, and child management all belong to `Container`. This avoids polluting every `Content` row with nullable infrastructure columns. It also maps naturally to the domain: a DiscussionContent owns its posts, a FolderContent owns its files, a FeedContent owns its entries, a GroupContent owns its members.

### 4. ContentItem as standalone table (not TPT child)

ContentItem is a standalone table with its own PK, not a subclass of Content. This avoids TPT join overhead on the hot item path and keeps the Content base table focused on containers only. The tradeoff is that cross-cutting queries spanning containers + items (e.g., "all content with label X") require two queries or a UNION — acceptable for the target scale.

### 5. Scale-appropriate pragmatism

At 2–50 person deployments, none of the classic RDBMS scaling problems apply. The design optimizes for developer clarity and extensibility over denormalized query performance.

---

## Consequences

### Positive

- **Single label infrastructure** — Labels work identically for containers and items via two link tables.
- **Container-level vector indexing** — Collection lifecycle is tied to container lifecycle. No orphaned vector collections.
- **Schema minimalism** — `Contents` (TPT base for containers) + per-container tables + `ContentItems` + optional child tables.
- **No orphan problem** — Since ContentItem is standalone, there is no orphan-row scenario that was a concern in the TPT-everything model.

### Negative

- **ContentItem queries require join to Container** — Every item query must join or filter by `ContainerId`. Acceptable at target scale.
- **Cross-cutting queries across containers + items need UNION** — e.g., "all entities with label Urgent" requires two passes.
- **No integer-friendly URLs yet** — GUID-only means longer URLs. `BusinessId` can be added later if needed.

### Neutral

- **Container abstraction** — Adds a level to the inheritance hierarchy. Earns its keep through concrete behavior (vector store, permissions, children).
- **Separate label tables per level** — Duplicates table schema but avoids index pollution between `Content` and `ContentItem` queries.

---

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| **Flat Content table with discriminator only** (no Container, no ContentItem) | No natural place for vector store / permission ownership; every query must filter by discriminator; no type-safe distinction between top-level and child entities without application logic |
| **ContentItem as TPT child of Content** | Would put containers and items in the same PK sequence; TPT join on every item query; risk of orphan rows; no clear benefit over standalone table |
| **Single Label table for all entities** | Polymorphic FK prevents clean FK enforcement; query plans degrade due to nullable columns and OR predicates |
| **Integer-only identity (no GUIDs)** | Prevents offline ID generation; merge/collision risk if data sources are combined; GUIDs are the canonical choice for distributed identity |
| **Per-type per-container BusinessId** | Adding integer sequences would give cleaner URLs but adds complexity (max+1 per scope, move semantics). No caller currently needs it — deferred until a concrete requirement emerges |
| **Move via FK update (reassign parent)** | Simpler than copy-and-delete but loses audit trail; not needed until move semantics are implemented |
| **CQRS / event-sourced content store** | Unnecessary complexity for 2–50 person enterprises; adds eventual consistency, replay, and snapshot management overhead with no clear benefit |

---

## Related

- [ADR-Storage-PgVector-Connector-01](docs/adr/01-Library-Storage-PgVector-Connector.md) — Vector store backend selection that Container-level indexing will use
- [ADR-UI-DualLayer-03](docs/adr/03-ADR-UI-DualLayer.md) — UI rendering layer built on this content model
- [ADR-Metrics-Platform-04](docs/adr/04-Metrics-Platform.md) — OpenTelemetry-driven metrics platform
- `AGENTS.md` — Coding conventions for the project (primary constructors, sealed classes, DI patterns)
