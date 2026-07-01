# Plan

> What's built (Done) and what's coming (Coming Up).

---

## Done

### Core Model

- **Content** — `Content` entity with unique `Name`, hierarchical parent/children, `ContentType` discriminator, `Status` lifecycle, `MetadataJson`, `SearchText`, timestamps. Self-referencing FK (`ParentId`) with restrict delete.
- **Post** — `Post` entity with `PostType` (post/thread/comment), optional `ContentId` parent, `AuthorUserId`, `IsSystemGenerated`, `MetadataJson`. FKs to Contents with restrict delete.
- **Label** — `Label` with `Name`, `NormalizedName` (unique), `Slug`, `Color`, `Description`. Many-to-many via `ContentLabels` and `PostLabels` join tables.
- **ContentRelationship** — Directed relationship between two content items with `RelationshipType` string and `MetadataJson`.
- **FileRecord** — 1-to-1 with Content: `FileName`, `ContentType`, `SizeBytes`, `ChecksumSha256`, `StorageProvider`, `BucketName`, `ObjectKey`.
- **MetricDefinition** — Named metric with `ContainerType`, `InstrumentKind`, `Aggregation`, `SourceField`, `Unit`, `DisplayUnit`, `Description`.
- **ContentTypes** constants — `FeedContainer`, `ForumContainer`, `FileContainer`, `EntityContainer`, `Directory`, `Dashboard`, `SystemFeed`, `KnowledgeBase`.
- **PostTypes** constants — `Post`, `Thread`, `Comment`.

### Business Concept Mapping

Business concepts map to container types through which component service is called, not through configuration:

| Concept | Service | Container Type | Children |
|---------|---------|----------------|----------|
| Feed / News / Incidents | `FeedComponentService` | `FeedContainer` | Content items + Posts |
| Forum / Discussions | `ForumComponentService` | `ForumContainer` | Posts (type `thread`) |
| File Library / Docs | `FileComponentService` | `FileContainer` | Content items + FileRecords |
| Entity Directory | `EntityComponentService` | `EntityContainer` | Content items with user-defined ContentType |

The component ID (e.g., `"CompanyNews"`, `"Machines"`) is the `Content.Name` lookup key. Each service hard-codes its `ContentTypes.*` constant. Container children use user-supplied ContentType strings (e.g., `"Outage"`, `"Machine"`, `"Team"`). Post type inference: replies with `contentId` → `Comment`, forum containers → `Thread`, otherwise → `Post`.

### Data Access

- `IWorkplaceIqStore` (97 lines) — CRUD for Content, Posts, Files, ContentRelationships, Labels, MetricDefinitions. All async with `CancellationToken`.
- `EfWorkplaceIqStore` (371 lines) — Full EF Core implementation in `WorkplaceIQ.AspNet`.
- `WorkplaceIqDbContext` — 8 DbSets (`Contents`, `Posts`, `Labels`, `PostLabels`, `ContentLabels`, `ContentRelationships`, `FileRecords`, `MetricDefinitions`). Unique indexes on `Name`/`NormalizedName`. FK constraints with `Restrict` delete. Database created via `EnsureCreated()`.

### Component Services

All services follow the same pattern: resolve a container by `Name`, auto-provision if missing (dev mode), return typed results. Auto-provisioning creates the container Content record with the hard-coded ContainerType as both ContentType and RendererKey. It does not auto-create APIs, permissions, AI indexes, search, or dashboards.

- **FeedComponentService** — Resolves feed containers, returns posts + content items + labels. `CreatePostAsync()` with label parsing.
- **ForumComponentService** — Thread-based posts. `CreateThreadAsync()` with initial post.
- **FileComponentService** — File container resolution, file upload with `IFormFile`, labels, metadata.
- **EntityComponentService** — Entity list/detail, entity creation with metadata/labels, relationships.
- **ComponentService** — Generic container resolution used by all above.
- **ContentService** — Thin wrapper over store for content CRUD with trimming.

### Tag Helpers

Six Tag Helpers registered via `@addTagHelper *, WorkplaceIQ.AspNet`. All use constructor injection, render semantic HTML with BEM-like classes, encode output via `HtmlEncoder`, and emit `data-*` attributes for JS interaction.

| TagHelper | Attributes | Behavior |
|-----------|------------|----------|
| `FeedTagHelper` | `id`, `title`, `system-managed`, `disable-*` | Resolves feed container, renders posts/content with action buttons |
| `ForumTagHelper` | `id`, `title`, `system-managed`, `disable-*` | Resolves forum container, renders threads |
| `FilesTagHelper` | `id`, `title`, `system-managed`, `disable-*` | Resolves files container, renders file cards |
| `EntityListTagHelper` | `id`, `title`, `type`, `system-managed`, `disable-*` | Resolves entity container, renders entity cards |
| `EntityTagHelper` | `id`, `title`, `type`, `container` | Single entity detail with relationships |
| `MetricTagHelper` | `name`, `source`, `content-type`, `source-field`, `window`, `unit`, `display-unit`, `container-type`, `container-id` | Builds `MetricRequest`, renders display value |

Interaction controls: `disable-add`, `disable-edit`, `disable-delete`, `disable-comment`, `disable-label`. Use `system-managed` for auto-provisioning.

### Metrics

- `IMetricProvider` interface with `MetricRequest → MetricResult` pipeline.
- 5 providers registered as singletons:
  - `ContentCountMetricProvider` — `workplaceiq.container.content.count`
  - `MetadataAggregationMetricProvider` × 4 — `workplaceiq.metadata.sum`, `.avg`, `.min`, `.max`
- `IMetricService` — resolves providers by name, computes results, supports time windows.
- OpenTelemetry integration via `"WorkplaceIQ"` meter.
- Unit conversion (e.g., seconds → hours via `display-unit`).

### File Storage

- `IFileObjectStorage` — `EnsureBucket`, `Upload`, `OpenRead`, `Delete`.
- `S3FileObjectStorage` — AWS SDK S3 implementation (compatible with MinIO).
- `FileStorageOptions` — `Provider`, `Endpoint`, `BucketName`, `AccessKey`, `SecretKey`, `UseSsl`.
- Auto-bucket creation: `EnsureBucketAsync()` checks existence via `DoesS3BucketExistV2Async`, creates via `PutBucketRequest` if missing. Called on every file upload.

### Web App (Demo)

- **Pages**: Dashboard (metrics cards + nav), News (posts with labels), Incidents (feed with metadata metrics), Discussions (threads with labels), Documents (file upload/download).
- **Controllers**: `HomeController` (page views + CreateFeedPost/CreateForumThread POST), `ContentController` (AddComment/AddLabel/Edit/Delete POST), `FilesController` (Upload/Download).
- **UI**: Bootstrap 5 + 770-line custom CSS design system (Inter font, BEM component classes, design tokens). Modal-based CRUD via `site.js`.
- **Seeded data**: 14 labels with colors, 4 news posts, 3 forum threads, 2 entities with relationship, 12 incident content items with metadata, 2 metric definitions.
- **JS interaction**: Single modal form (`#iqItemActionModal`) handles comment/add-label/edit/delete across all item types. `data-iq-action` / `data-iq-type` / `data-iq-id` attributes drive behavior.

### Infrastructure

- **Docker Compose** — `workplaceiq-web` (port 4792) + `minio` (ports 9000/9001) with named volumes.
- **Dockerfile** — Multi-stage .NET 10 build.
- **.NET Aspire** — `WorkplaceIQ.AppHost` orchestrates PostgreSQL + MinIO for development.
- **CI** — GitHub Actions workflow in `.github/workflows/ci.yml`. Restore → Build → Test on .NET 10.
- **Service defaults** — OpenTelemetry metrics/tracing, health checks, resilience pipelines, service discovery.

### Tests

25+ NUnit tests across 10 files:

| Test File | Tests |
|-----------|-------|
| `FeedComponentServiceTests` | 11 — auto-provisioning, content items, production mode, missing ID, trimming, post creation, invalid input, label normalization |
| `FileComponentServiceTests` | 4 — auto-provisioning, upload with labels, per-container filtering, stream readback |
| `EntityComponentServiceTests` | 4 — auto-provisioning, metadata+labels, per-list filtering, relationships |
| `ContentServiceTests` | 3 — create with trimming, parent-based filtering, missing content |
| `MetricServiceTests` | 4 — content count with window, metadata sum + display unit, series across containers, unknown metric |
| `FeedTagHelperTests` | 5 — rendering, empty state, XSS, content items, system-managed, disable attrs |
| `ForumTagHelperTests` | 3 — rendering, empty, XSS |
| `FilesTagHelperTests` | 3 — rendering, empty, XSS |
| `EntityTagHelperTests` | 3 — entity list with relationships, XSS, empty |
| `MetricTagHelperTests` | 2 — request building, display value rendering |

Test doubles (`InMemoryWorkplaceIqStore`, `InMemoryFileObjectStorage`, recording wrappers, `TagHelperOutputFactory`, `TestHostEnvironment`) in `TestDoubles/`.

---

## Coming Up

### AI & Intelligence

- **Vector/Semantic Search** — Embed content, posts, and files. Store in pgvector, Azure AI Search, Qdrant, or Redis. Permission-aware retrieval. Hybrid (keyword + vector) search.
- **AI Chat** — `<iq-ai-chat>` tag helper for RAG-based natural language querying across containers, feeds, forums, files, entities. Source-cited answers with follow-up suggestions. Metric querying via tools.
- **Summaries & Digests** — Daily feed summaries, weekly department digests, content/thread/file summaries. Scheduled or on-demand. Stored as system posts with source references.
- **Insights Engine** — Trend detection, anomaly identification, emerging topic discovery. Metric-driven insight templates. AI-generated explanations with severity, confidence, category, recommendation. Acknowledge/dismiss workflow.

### UI Components

- **Dashboards** — `<iq-dashboard>` tag helper composing metric cards, trend charts, grouped breakdowns, recent activity, AI insights, natural language chart explanations.
- **System-Generated Views** — Virtual containers from filters/rules/queries (e.g., "Last 7 Days Outages"). Behave like normal containers. Permission inheritance from source.
- **Additional Tag Helpers** — `<iq-ai-chat>`, `<iq-ai-summary>`, `<iq-insights>`, `<iq-dashboard>`, `<iq-chart>`.

### Platform Features

- **Permissions & Security** — Tenant isolation at data/search/vector/analytics levels. Container/content/post/field-level permissions. CRUD + comment + label + AI + metrics + insights actions. Scoped API access with service accounts.
- **Multi-Tenant** — Isolated databases or shared with tenant filtering. Tenant-aware routing, search, vector indexing. Site management within tenants.
- **Full-Text Search** — Keyword search across containers, metadata/label filters with facets, container-scoped and site-wide. Result types: containers, content, posts, files, comments, users, insights, metrics.
- **Metadata Schemas** — Versioned field definitions per content type. Field types: string, number, integer, boolean, date, datetime, duration, choice, multi-choice, geo-location, json, references. Form generation, validation, analytics-driven schema.
- **Knowledge Base** — Full `KnowledgeBase` container type with hierarchy, rich content, search, and AI chat.

### Infrastructure & Ecosystem

- **Admin UI** — Container, schema, metric, and user management. Usage analytics and health monitoring.
- **Custom Renderers & Container Types** — Pluggable renderers per container type. Custom metric providers, metadata field types, AI providers, vector stores.
- **Audit Logging** — Track creation/modification of all entity types. External API writes. AI-generated artifact tracking.
- **Deployment Templates** — Azure / on-premises. Helm charts, ARM/Bicep templates. Multi-region support.
- **Component Marketplace** — Reusable intranet templates and custom components.
