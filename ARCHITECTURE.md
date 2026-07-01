# Architecture

> What's built and how it fits together.

## ADR Status

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-01](docs/adr/01-Library-Storage-PgVector-Connector.md) | SK PgVector Connector & Npgsql Compatibility | **Landed in code** |
| [ADR-02](docs/adr/02-Domain-Content-Modeling.md) | Unified Polymorphic Content Model | **Landed in code** |
| [ADR-03](docs/adr/03-ADR-UI-DualLayer.md) | Dual UI Layer — Tag Helpers & Dedicated Controllers | **Next** |
| [ADR-04](docs/adr/04-Metrics-Platform.md) | OpenTelemetry-Driven Metrics Platform | **Future** |

## Core Model (ADR-02)

```
Content (abstract, TPT base)
├── Container (abstract)
│   ├── DiscussionContent     — forum/discussion
│   ├── FolderContent         — file library
│   ├── FeedContent           — news/feed
│   └── GroupContent          — entity directory (+ GroupType)

ContentItem (sealed, standalone)   — own PK, ContainerId FK
├── topic       — forum posts
├── feed_entry  — feed items
├── file        — files          (+ optional ContentFile child)
└── member      — directory entities

ContentFile (sealed, 1:1 child)   — file storage metadata
```

- **Content** (abstract) — TPT base. `Id` (GUID PK), `CreatedAt`/`CreatedBy`/`ModifiedAt`/`ModifiedBy`.
- **Container** (abstract) — extends `Content`. `Name`, `Title`, `Description`, `VectorCollectionName`, `RendererKey`, `Status`, `SettingsJson`, `IsSystemGenerated`. Children via `Items` collection.
- **ContentItem** (sealed) — Own PK (`Guid`), `ContainerId` FK → `Content.Id`, `Discriminator` (`topic`/`feed_entry`/`file`/`member`), `Name`, `Title`, `Body`, `AuthorUserId`, `Status`, `PublishedAt`, `ContentData` (JSON).
- **ContentFile** (sealed, 1:1 child) — `FileName`, `ContentType`, `SizeBytes`, `ChecksumSha256`, `StorageProvider`, `BucketName`, `ObjectKey`.
- **Label** — `Name`, `NormalizedName` (unique), `Slug`, `Color`, `Description`. Many-to-many via `ContentLabels` and `ContentItemLabels`.
- **ContentRelationship** — Directed relationship between two `Content` rows with `RelationshipType`.
- **MetricDefinition** — `ContainerType`, `InstrumentKind`, `Aggregation`, `SourceField`, `Unit`, `DisplayUnit`, `Description`.
- **ClassifiedItem** — One-per-ContentItem classification with `SignalLabel`, `Score`, `Confidence`.
- **ContentTypes** constants — `FeedContainer`, `ForumContainer`, `FileContainer`, `EntityContainer`.
- **Discriminators** — `topic`, `feed_entry`, `file`, `member`.

## Business Concept Mapping

Business concepts map to container types through which component service is called, not through configuration:

| Concept | Service | Container Type | Children |
|---------|---------|----------------|----------|
| Feed / News / Incidents | `FeedComponentService` | `FeedContent` | ContentItems (discriminator `feed_entry`) |
| Forum / Discussions | `ForumComponentService` | `DiscussionContent` | ContentItems (discriminator `topic`) |
| File Library / Docs | `FileComponentService` | `FolderContent` | ContentItems (discriminator `file` + optional ContentFile) |
| Entity Directory | `EntityComponentService` | `GroupContent` | ContentItems (discriminator `member`) |

The component ID (e.g., `"CompanyNews"`, `"Machines"`) is the `Container.Name` lookup key. Each service hard-codes its container type. All child items share the `ContentItem` table with discriminator-based routing.

## Data Access

- **`IWorkplaceIqStore`** — Typed CRUD for containers, items, files, labels, classifications, relationships, metric definitions. All async with `CancellationToken`.
- **`EfWorkplaceIqStore`** — EF Core implementation via `IDbContextFactory<WorkplaceIqDbContext>` for thread safety.
- **`WorkplaceIqDbContext`** — TPT mapping for containers (`Contents` base + `DiscussionContents`/`FolderContents`/`FeedContents`/`GroupContents`), standalone `ContentItems` + `ContentFiles`, link tables (`ContentLabels`, `ContentItemLabels`, `ContentRelationships`), `ClassifiedItems`, `MetricDefinitions`. Database created via `EnsureCreated()`.

## Component Services

All services resolve a container by `Name`, auto-provision if missing (dev mode), return typed results. Auto-provisioning creates the container record with computed `RendererKey` — it does not auto-create APIs, permissions, AI indexes, search, or dashboards.

- **`FeedComponentService`** — Resolves `FeedContent` containers, returns items with discriminator `feed_entry`. `CreatePostAsync()` with label parsing.
- **`ForumComponentService`** — Resolves `DiscussionContent` containers, threads via items with discriminator `topic`.
- **`FileComponentService`** — Resolves `FolderContent` containers, file upload with `ContentItem` (discriminator `file`) + `ContentFile` child row.
- **`EntityComponentService`** — Resolves `GroupContent` containers, entity list/detail with discriminator `member`.
- **`ComponentService`** — Generic type-aware container resolution used by all above.
- **`ContainerService`** — CRUD for containers.
- **`ContentItemService`** — CRUD for items within a container.

## Tag Helpers

Six Tag Helpers registered via `@addTagHelper *, WorkplaceIQ.AspNet`. All use constructor injection, render semantic HTML with BEM-like classes, encode output via `HtmlEncoder`, and emit `data-*` attributes for JS interaction.

| TagHelper | Attributes | Behavior |
|-----------|------------|----------|
| `FeedTagHelper` | `id`, `title`, `system-managed`, `disable-*` | Resolves `FeedContent` container, renders items with action buttons |
| `ForumTagHelper` | `id`, `title`, `system-managed`, `disable-*` | Resolves `DiscussionContent` container, renders threads |
| `FilesTagHelper` | `id`, `title`, `system-managed`, `disable-*` | Resolves `FolderContent` container, renders file cards |
| `EntityListTagHelper` | `id`, `title`, `type`, `system-managed`, `disable-*` | Resolves `GroupContent` container, renders entity cards |
| `EntityTagHelper` | `id`, `title`, `type`, `container` | Single entity detail with relationships |
| `MetricTagHelper` | `name`, `source`, `content-type`, `source-field`, `window`, `unit`, `display-unit`, `container-type`, `container-id` | Builds `MetricRequest`, renders display value |

Interaction controls: `disable-add`, `disable-edit`, `disable-delete`, `disable-comment`, `disable-label`. Use `system-managed` for auto-provisioning.

## Metrics Platform

- **`IMetricProvider`** interface with `MetricRequest → MetricResult` pipeline.
- 5 providers registered as singletons:
  - `ContentCountMetricProvider` — `workplaceiq.container.content.count`
  - `MetadataAggregationMetricProvider` × 4 — `workplaceiq.metadata.sum`, `.avg`, `.min`, `.max`
- **`IMetricService`** — resolves providers by name, computes results, supports time windows.
- OpenTelemetry integration via `"WorkplaceIQ"` meter.
- Unit conversion (e.g., seconds → hours via `display-unit`).

## File Storage

- **`IFileObjectStorage`** — `EnsureBucket`, `Upload`, `OpenRead`, `Delete`.
- **`S3FileObjectStorage`** — AWS SDK S3 implementation (compatible with MinIO).
- **`FileStorageOptions`** — `Provider`, `Endpoint`, `BucketName`, `AccessKey`, `SecretKey`, `UseSsl`.
- **`FileObject`** record wraps `(ContentItem, ContentFile)` pair.
- Auto-bucket creation: `EnsureBucketAsync()` checks existence via `DoesS3BucketExistV2Async`, creates via `PutBucketRequest` if missing. Called on every file upload.

## Storage Providers

Provider selection via `Storage:Provider` in `appsettings.json` (default: `sqlite`).

| Provider | EF Core | Vector Store | Connection String Key |
|----------|---------|-------------|----------------------|
| `sqlite` | `UseSqlite` | `SqliteVectorStore` | `ConnectionStrings:Sqlite` |
| `pgvector` | `UseNpgsql` | `PostgresVectorStore` | `ConnectionStrings:Npgsql` / `PgVector` |
| `sqlserver` | `UseSqlServer` | `SqlServerVectorStore` | `ConnectionStrings:SqlServer` |
| `inmemory` | `UseInMemoryDatabase` | `InMemoryVectorStore` | none |

## Web App (Demo)

- **Pages**: Dashboard (metrics cards + nav), News (posts with labels), Incidents (feed with metadata metrics), Discussions (threads with labels), Documents (file upload/download).
- **Controllers**: `HomeController` (page views + CreateFeedPost/CreateForumThread POST), `ContentController` (AddComment/AddLabel/Edit/Delete POST), `FilesController` (Upload/Download).
- **UI**: Bootstrap 5 + custom CSS design system (Inter font, BEM component classes, design tokens). Modal-based CRUD via `site.js`.
- **Seeded data**: 14 labels with colors, 4 news posts, 3 forum threads, 2 entities with relationship, 12 incident content items with metadata, 2 metric definitions.
- **JS interaction**: Single modal form (`#iqItemActionModal`) handles comment/add-label/edit/delete across all item types. `data-iq-action` / `data-iq-type` / `data-iq-id` attributes drive behavior.

## Infrastructure

- **Docker Compose** — `workplaceiq-web` (port 4792) + `minio` (ports 9000/9001) with named volumes.
- **Dockerfile** — Multi-stage .NET 10 build.
- **.NET Aspire** — `WorkplaceIQ.AppHost` orchestrates PostgreSQL + MinIO for development.
- **CI** — GitHub Actions workflow (restore → build → test on .NET 10).
- **Service defaults** — OpenTelemetry metrics/tracing, health checks, resilience pipelines, service discovery.

## Tests

46 NUnit tests across 10 files:

| Test File | Tests |
|-----------|-------|
| `FeedComponentServiceTests` | 11 |
| `FileComponentServiceTests` | 4 |
| `EntityComponentServiceTests` | 4 |
| `ContentServiceTests` | 4 |
| `MetricServiceTests` | 4 |
| `FeedTagHelperTests` | 5 |
| `ForumTagHelperTests` | 3 |
| `FilesTagHelperTests` | 3 |
| `EntityTagHelperTests` | 3 |
| `MetricTagHelperTests` | 2 |

Test doubles (`InMemoryWorkplaceIqStore`, `InMemoryFileObjectStorage`, recording wrappers, `TagHelperOutputFactory`, `TestHostEnvironment`) in `TestDoubles/`.
