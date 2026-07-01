# PLAN: ADR 02 Domain Content Model Implementation

## Overview

Refactor the monolithic `Content` class into a **table-per-type (TPT)** polymorphic hierarchy: a shared `Content` base table, per-container-type tables (`DiscussionContents`, `FolderContents`, `FeedContents`, `GroupContents`), a `ContentItems` table for all child items (discriminator-based), and an optional child table `ContentFiles` for file storage metadata.

Since we are 0.x, there is **no data migration** — we build the new schema from scratch. The old flat `Content` table and standalone `Post`/`FileRecord` entities are replaced entirely.

---

## 1. Schema Design

> TPT inheritance (`UseTptMappingStrategy()`) **does not need an explicit Discriminator column** — EF Core infers the concrete type by which child table has a matching row. The `Discriminator` column is intentionally omitted from `Content`; the C# model likewise has no `Discriminator` property.

### 1.1 Content (shared base — one row per container)

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` PK | |
| `CreatedAt` | `DateTime` (UTC) | |
| `CreatedBy` | `string(128)?` | |
| `ModifiedAt` | `DateTime` (UTC) | |
| `ModifiedBy` | `string(128)?` | |

Shared infrastructure via link tables (Id FK → Content.Id):
- `ContentLabels` → `Label`
- `ContentMetadata` → key/value
- `ContentMetrics` → metric values

### 1.2 Per-Container-Type Tables

Each table has `Id = Guid PK/FK → Content.Id`.

**DiscussionContents**

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` PK/FK | |
| `BusinessId` | `int` | Application-managed (max+1 per container type) |
| `Name` | `string(256)` | Unique, URL-safe slug |
| `Title` | `string(256)` | Display title |
| `Description` | `string?` | |
| `VectorCollectionName` | `string?` | |
| `RendererKey` | `string(128)?` | |
| `Status` | `string(32)` | `active`, `archived` |
| `SettingsJson` | `string?` | |
| `IsSystemGenerated` | `bool` | |

**FolderContents** (same structure)

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` PK/FK | |
| `BusinessId` | `int` | Application-managed (max+1 per container type) |
| `Name` | `string(256)` | |
| `Title` | `string(256)` | |
| `Description` | `string?` | |
| `VectorCollectionName` | `string?` | |
| `RendererKey` | `string(128)?` | |
| `Status` | `string(32)` | |
| `SettingsJson` | `string?` | |
| `IsSystemGenerated` | `bool` | |

**FeedContents** (same structure)

**GroupContents** (same structure + GroupType)

| Column | Type | Notes |
|---|---|---|
| ...same as above... | | |
| `GroupType` | `string(128)?` | e.g., `Machine`, `Team` |

### 1.3 ContentItems (all child items)

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` PK | **Standalone** — does NOT extend Content |
| `ContainerId` | `Guid` FK → Content.Id | Parent container |
| `Discriminator` | `string(32)` | `topic`, `feed_entry`, `file`, `member` |
| `BusinessId` | `int` | Application-managed (max+1 per container) |
| `Title` | `string(256)` | Post/discussion title, file name, member name |
| `Body` | `string?` | Post body, file description, member description |
| `AuthorUserId` | `string(128)?` | |
| `Status` | `string(32)` | `active`, `archived` |
| `PublishedAt` | `DateTime?` | |
| `ContentData` | `string?` | Flexible payload for type-specific data (JSON text; native jsonb on PostgreSQL) |
| `CreatedAt` | `DateTime` (UTC) | |
| `CreatedBy` | `string(128)?` | |
| `ModifiedAt` | `DateTime` (UTC) | |
| `ModifiedBy` | `string(128)?` | |

### 1.4 Optional Child Tables (extend ContentItems via Id FK)

Only created when a discriminator needs typed columns beyond what `ContentData` (JSON text) provides.

**ContentFiles** (for `discriminator = 'file'`)

| Column | Type |
|---|---|
| `Id` | `Guid` PK/FK → ContentItems |
| `FileName` | `string(512)` |
| `ContentType` | `string(256)` |
| `SizeBytes` | `long` |
| `ChecksumSha256` | `string(128)?` |
| `StorageProvider` | `string(64)` |
| `BucketName` | `string(256)` |
| `ObjectKey` | `string(1024)` |

Discriminators without a child table (`topic`, `feed_entry`, `member`) store all type-specific data in `ContentItems.ContentData` (JSON text).

---

## 2. Domain Model (C# Classes)

### Namespace `WorkplaceIQ.Content`

```
Content (abstract)
├── Container (abstract)
│   ├── DiscussionContent (sealed)
│   ├── FolderContent (sealed)
│   ├── FeedContent (sealed)
│   └── GroupContent (sealed, +GroupType)
├── ContentItem (concrete — discriminator-based, has Title/Body/etc.)
└── ContentFile (sealed, 1:1 child of ContentItem — file storage metadata)
```

**Content.cs** — shared base:
```csharp
public abstract class Content
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
}
```

**Container.cs** — abstract, common container fields:
```csharp
public abstract class Container : Content
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VectorCollectionName { get; set; }
    public string? RendererKey { get; set; }
    public string Status { get; set; } = "active";
    public string? SettingsJson { get; set; }
    public bool IsSystemGenerated { get; set; }
    public ICollection<ContentItem> Items { get; set; } = [];
}
```

Concrete containers: `DiscussionContent`, `FolderContent`, `FeedContent`, `GroupContent` (GroupContent adds `string? GroupType`).

**ContentItem.cs** — concrete, discriminator-based, has common post fields:
```csharp
public class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContainerId { get; set; }
    public Container? Container { get; set; }
    public string Discriminator { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? AuthorUserId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? PublishedAt { get; set; }
    public string? ContentData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public ICollection<ContentItemLabel> Labels { get; set; } = [];
}
```

**ContentFile.cs** (optional child of ContentItem — file storage metadata):
```csharp
public class ContentFile
{
    public Guid Id { get; set; }
    public ContentItem? ContentItem { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
}
```

### Namespace `WorkplaceIQ.Labels`

- `ContentLabel` — link table (ContentId → Content.Id, LabelId → Label.Id) — unchanged shape but now FK to Content base
- `ContentItemLabel` — new (ContentItemId → ContentItems.Id, LabelId → Label.Id) — for item-level labels
- `Label` — unchanged
- `PostLabel` — **removed** (replaced by `ContentItemLabel`)

### Namespace `WorkplaceIQ.Metrics`

- `ContentMetric` — link table (ContentId, MetricDefinitionId, Value, Timestamp)
- `ContentItemMetric` — link table (ContentItemId, MetricDefinitionId, Value, Timestamp)
- `ContentMetadata` — link table (ContentId, Key, Value) — replaces `Content.MetadataJson`
- `ContentItemMetadata` — link table (ContentItemId, Key, Value) — replaces item-level JSON metadata

---

## 3. Data Access Layer

### 3.1 DbContext (`WorkplaceIqDbContext`)

- `DbSet<Content> Contents` — base table
- `DbSet<DiscussionContent> DiscussionContents`
- `DbSet<FolderContent> FolderContents`
- `DbSet<FeedContent> FeedContents`
- `DbSet<GroupContent> GroupContents`
- `DbSet<ContentItem> ContentItems`
- `DbSet<ContentFile> ContentFiles`
- `DbSet<ContentLabel> ContentLabels`
- `DbSet<ContentItemLabel> ContentItemLabels`
- `DbSet<ContentMetric> ContentMetrics`
- `DbSet<ContentItemMetric> ContentItemMetrics`
- `DbSet<ContentMetadata> ContentMetadata`
- `DbSet<ContentItemMetadata> ContentItemMetadata`

### 3.2 Entity Configuration (TPT mapping)

```csharp
// ContentConfiguration: TPT base
entity.UseTptMappingStrategy();

// Container type configurations:
// entity.ToTable("DiscussionContents");
// entity.HasOne(d => (Content)d).WithOne().HasForeignKey<DiscussionContent>(d => d.Id);

// ContentItemConfiguration
entity.ToTable("ContentItems");
// Container.Items → ContentItem.ContainerId FK:
// entity.HasOne(ci => ci.Container).WithMany(c => c.Items).HasForeignKey(ci => ci.ContainerId);

// ContentFileConfiguration
entity.ToTable("ContentFiles");
entity.HasOne(f => f.ContentItem).WithOne().HasForeignKey<ContentFile>(f => f.Id);
```

### 3.3 Store Interface (`IWorkplaceIqStore`)

Replace flat Content CRUD with typed methods:

```csharp
// Container queries
Task<T?> GetContainerByIdAsync<T>(Guid id, CancellationToken ct = default) where T : Container;
Task<T?> GetContainerByNameAsync<T>(string name, CancellationToken ct = default) where T : Container;
Task<T?> GetContainerByBusinessIdAsync<T>(int businessId, CancellationToken ct = default) where T : Container;
Task<IReadOnlyList<T>> GetContainersByTypeAsync<T>(CancellationToken ct = default) where T : Container;
Task<T> CreateContainerAsync<T>(T container, CancellationToken ct = default) where T : Container;
Task<T> UpdateContainerAsync<T>(T container, CancellationToken ct = default) where T : Container;
Task DeleteContainerAsync(Guid id, CancellationToken ct = default);

// ContentItem queries
Task<ContentItem?> GetItemByIdAsync(Guid id, CancellationToken ct = default);
Task<ContentItem?> GetItemByBusinessIdAsync(Guid containerId, int businessId, CancellationToken ct = default);
Task<IReadOnlyList<ContentItem>> GetItemsByContainerAsync(Guid containerId, string? discriminator = null, CancellationToken ct = default);
Task<ContentItem> CreateItemAsync(ContentItem item, CancellationToken ct = default);
Task<ContentItem> UpdateItemAsync(ContentItem item, CancellationToken ct = default);
Task DeleteItemAsync(Guid id, CancellationToken ct = default);

// Child table queries
Task<ContentFile?> GetContentFileByItemIdAsync(Guid itemId, CancellationToken ct = default);
Task<ContentFile> CreateContentFileAsync(ContentFile file, CancellationToken ct = default);

// Classification (preserve one-per-ContentItem invariant)
Task<ClassifiedItem> UpsertClassifiedItemAsync(ClassifiedItem item, CancellationToken ct = default);
// existing classification queries unchanged — ContentId now refers to ContentItem.Id

// Labels, Metrics, Relationships — adapt FKs to new tables
```

### 3.4 Store Implementations

- `EfWorkplaceIqStore` — rewrite from scratch for new schema
- `InMemoryWorkplaceIqStore` — rewrite from scratch for test doubles

---

## 4. Service Layer Migration

### 4.1 Component Services

All component services (`FeedComponentService`, `ForumComponentService`, `FileComponentService`, `EntityComponentService`) currently return `ComponentResult` with `Content.Content? Container`. Change to return their typed container:

| Service | Old | New |
|---|---|---|
| `FeedComponentService` | `Content?` | `FeedContent?` |
| `ForumComponentService` | `Content?` | `DiscussionContent?` (Forum → DiscussionContent) |
| `FileComponentService` | `Content?` | `FolderContent?` |
| `EntityComponentService` | `Content?` | `GroupContent?` |

The `ComponentService.ResolveAsync` method becomes type-aware — it creates the correct container type based on the requested type parameter.

### 4.2 ContentService

Currently works with flat `Content`. Split into:

- `ContainerService` — CRUD for containers, vector collection management
- `ContentItemService` — CRUD for items within a container

Or keep as `ContentService` with overloaded methods. TBD during implementation.

### 4.3 Metric Providers

`MetricProviderBase.GetContainerItemsAsync` currently returns `(Content Container, IReadOnlyList<Content> Items)`. Change to `(Container Container, IReadOnlyList<ContentItem> Items)`.

### 4.4 Classification / SignalFlow

`ClassifiedItem.ContentId` currently refers to `Content.Id`. After refactoring, classification applies to `ContentItem` (not container). Change FK to `ContentItem.Id`. The one-per-content invariant becomes one-per-ContentItem.

### 4.5 File Operations

`FileComponentService.UploadAsync` currently creates a `Content` + `FileRecord`. Change to create a `ContentItem` (discriminator = `file`) + associated `ContentFile` child row.

`FileObject` record currently wraps `Content.Content` + `FileRecord`. Change to `ContentItem` + `ContentFile`. `IFileObjectStorage` remains unchanged.

---

## 5. Web Layer Migration

### 5.1 Controllers

`ContentController` currently works with `itemType == "content"` vs `"post"`. Change to work with `ContentItem` directly:

- `AddComment` → creates a `ContentItem` (discriminator = `topic`) under the same container
- `AddLabel` → checks item type and adds to `ContentLabel` (container) or `ContentItemLabel` (item)
- `Edit` → updates `ContentItem` directly (Title, Body, Status)
- `Delete` → soft-deletes `ContentItem` or `Container`

### 5.2 Tag Helpers

Tag helper result types update their container reference:

| Tag Helper | Old Container Type | New Container Type |
|---|---|---|
| `FeedTagHelper` | `Content?` | `FeedContent?` |
| `ForumTagHelper` | `Content?` | `DiscussionContent?` |
| `FilesTagHelper` | `Content?` | `FolderContent?` |
| `EntityTagHelper` | `Content?` | `GroupContent?` |

### 5.3 DemoDataSeeder

Rewrite to use new model:
- `FeedComponentService` → creates `FeedContent` container + `ContentItem` (feed_entry) items
- `ForumComponentService` → creates `DiscussionContent` container + `ContentItem` (topic) with Title/Body on ContentItem
- `EntityComponentService` → creates `GroupContent` container + `ContentItem` (member) items
- Labels, metrics, incidents → use new models

---

## 6. Test Strategy

### 6.1 InMemory Store

Rewrite `InMemoryWorkplaceIqStore` to use typed lists:
- `List<DiscussionContent> DiscussionContents`
- `List<FolderContent> FolderContents`
- `List<FeedContent> FeedContents`
- `List<GroupContent> GroupContents`
- `List<ContentItem> Items`
- `List<ContentFile> ContentFiles`
- `List<ContentLabel> ContentLabels`
- `List<ContentItemLabel> ContentItemLabels`
- etc.

### 6.2 Existing Tests

| Test File | Required Changes |
|---|---|
| `ContentServiceTests.cs` | Replace `Content.Content` with `ContentItem` |
| `ClassificationServiceTests.cs` | Replace `Content.Content` with `ContentItem` |
| `FeedComponentServiceTests.cs` | Use `FeedContent` container, `ContentItem` posts |
| `ForumTagHelperTests.cs` | Use `DiscussionContent` container |
| `FilesTagHelperTests.cs` | Use `FolderContent` container |
| `EntityTagHelperTests.cs` | Use `GroupContent` container |
| `MetricServiceTests.cs` | Update references |
| `MetricTagHelperTests.cs` | Update references |

---

## 7. Implementation Order

### Step 1 — Domain Model Types
Create new files:
- `Content/Content.cs` (abstract)
- `Content/Container.cs` (abstract)
- `Content/DiscussionContent.cs` (sealed)
- `Content/FolderContent.cs` (sealed)
- `Content/FeedContent.cs` (sealed)
- `Content/GroupContent.cs` (sealed)
- `Content/ContentItem.cs` (concrete, with Title/Body/etc.)
- `Content/ContentFile.cs` (child — file storage metadata, replace Files/FileRecord.cs)
- `Content/ContentLabel.cs` (update FK)
- `Content/ContentItemLabel.cs` (new)
- `Content/ContentMetadata.cs` (new)
- `Content/ContentItemMetadata.cs` (new)
- `Content/ContentMetric.cs` (new)
- `Content/ContentItemMetric.cs` (new)
- Remove old `Content/Content.cs`, `Content/ContentTypes.cs`, `Posts/Post.cs`, `Posts/PostTypes.cs`, `Files/FileRecord.cs`, `Labels/PostLabel.cs`, `Files/FileObject.cs`

### Step 2 — EF Core + Configurations
- Create `ContentConfiguration`, `DiscussionContentConfiguration`, `FolderContentConfiguration`, `FeedContentConfiguration`, `GroupContentConfiguration`
- Create `ContentItemConfiguration`, `ContentFileConfiguration`
- Create configurations for new link tables
- Update `WorkplaceIqDbContext`

### Step 3 — Store Interface + Implementations
- Rewrite `IWorkplaceIqStore`
- Rewrite `EfWorkplaceIqStore`
- Rewrite `InMemoryWorkplaceIqStore`

### Step 4 — Service Layer
- Update `ContainerService`/`ContentItemService` (was `ContentService`)
- Update `ComponentService` and all component services
- Update metric providers
- Update SignalFlow pipeline (classifier references)

### Step 5 — Web Layer
- Update `ContentController`
- Update tag helpers (`FeedTagHelper`, `ForumTagHelper`, `FilesTagHelper`, `EntityTagHelper`, `EntityListTagHelper`)
- Update `DemoDataSeeder`
- Update renderers (`ComponentHtmlRenderer`, `LabelHtmlRenderer`)
- Update views minimally (model type references)

### Step 6 — Tests
- Update all test files to use new model
- Verify `dotnet test --configuration Release` passes

### Step 7 — Verify No Obsolete Types Remain
- Confirm all old types (`Content`, `ContentTypes`, `Post`, `PostTypes`, `FileRecord`, `PostLabel`, `FileContentTypes`, `FileObject`) have been removed in Step 1 and no stale references exist
- `dotnet build` passes with zero obsolete-type warnings

---

## 8. Decisions Log

| Question | Decision |
|---|---|
| Inheritance strategy | **TPT** — `Content` base table, per-concrete-type tables for containers (`DiscussionContents`, `FolderContents`, `FeedContents`, `GroupContents`). Separate `ContentItems` table (standalone, does NOT extend Content) with optional child table `ContentFiles` for file storage metadata. |
| Container types | `DiscussionContent`, `FolderContent`, `FeedContent`, `GroupContent`. ForumContainer → DiscussionContent (with `RendererKey = "forum"`). EntityContainer → GroupContent. |
| Item discriminators | `topic`, `feed_entry`, `file`, `member`. Title/Body/AuthorUserId live on `ContentItem` directly (most item types need them). |
| `ContentFile` | Only child table — for discriminator `file`, holds file storage metadata. Formerly `FileRecord` in `WorkplaceIQ.Files` namespace. |
| No `ContentTopics` child table | Discussion posts use Title/Body on `ContentItem`. Labels (via `ContentItemLabel`) serve as hashtags/topics for items. |
| `FeedEntry` / `Member` | No child table — use `ContentData` (JSON text) for any type-specific extras. |
| `ContentItem` extends `Content`? | **No** — standalone table with its own PK + ContainerId FK. Tradeoff: queries spanning containers + items (e.g., "all labeled Urgent") require two separate queries or a UNION. Acceptable for 2–50 person enterprises. |
| `Post` / `PostLabel` | Removed — replaced by `ContentItem` + `ContentItemLabel` |
| Data migration | **Not needed** — 0.x, clean slate |
| C# class name for `Folders` table | `FolderContent` (table name `FolderContents`) |
| Forum → DiscussionContent | ForumContainer becomes a `DiscussionContent` entry with `RendererKey = "forum"`. `ForumComponentService` wraps `DiscussionContent` queries. |
| `ContentRelationship` | References `Content.Id` only (container-level). No item-level relationships for now. |
| `ClassifiedItem.ContentId` | References `ContentItem.Id` (classification applies to items, not containers) |
