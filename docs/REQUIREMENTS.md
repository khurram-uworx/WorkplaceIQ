> **Historical** — Pre-development requirements document (June 2026).  
> See [README.md](../README.md) for current state and [PLAN.md](PLAN.md) for upcoming work.

# WorkplaceIQ Requirements

## 1. Product Vision

WorkplaceIQ is a metadata-driven, AI-native intranet and workplace intelligence platform.

The product allows a vendor or implementation team to compose customized company intranets using reusable declarative components such as feeds, forums, file libraries, directories, dashboards, business entities, and analytics views.

The key idea is that all visible workplace applications are built on a small set of abstract primitives underneath:

```text
Container
  └── Content
        └── Post / Item / Event
              └── Metadata
              └── Labels
              └── Comments
              └── Relationships
```

The platform should not treat “Newsfeed”, “Forum”, “Files”, “Outages”, “HR Policies”, or “Maintenance Issues” as completely separate systems. Instead, these should be named compositions over generic platform primitives.

The long-term objective is to build a SharePoint-like intranet platform for the AI era, where every content item is searchable, measurable, conversational, and insight-ready by default.

---

## 2. Working Product Name

**WorkplaceIQ**

Possible positioning:

> Build intelligent intranets from reusable content, collaboration, metadata, and AI components.

Alternative positioning:

> A metadata-driven workplace intelligence platform where every business activity becomes searchable, measurable, and AI-ready.

---

## 3. Core Concept

A customer intranet should be composed declaratively inside an ASP.NET MVC / Razor application using custom Tag Helpers or equivalent server-side components.

Example:

```html
<iq-newsfeed id="PowerOutages"
             title="Power Outages in Factory"
             allow-comments="true"
             ai-enabled="true" />

<iq-forum id="MaintenanceHelpdesk"
          title="Maintenance Helpdesk"
          allow-comments="true"
          ai-summary="daily" />

<iq-files id="HRPolicies"
          title="HR Policies"
          allow-chat="true" />

<iq-entity type="Machine"
           title="Factory Machines"
           ai-profile="true" />
```

Behind the scenes, each tag should create or bind to the required platform configuration, data structures, permissions, APIs, renderers, AI indexing, analytics, and metadata definitions.

---

## 4. Main Product Goals

1. Allow rapid composition of customized intranets for different companies.
2. Provide generic content and collaboration primitives instead of hardcoding every business use case.
3. Allow business-specific meaning to emerge through metadata, labels, relationships, and configuration.
4. Make all content AI-ready by default through vectorization, summarization, chat, and insight generation.
5. Provide generic metrics that can be mapped to business-specific interpretations.
6. Support both human-generated and system-generated content views.
7. Provide a developer-friendly model using ASP.NET MVC, Razor Pages, and Tag Helpers.
8. Allow external business systems to add content, posts, metadata, and events through APIs.

---

## 5. Abstract Domain Model

### 5.1 Tenant

Represents a customer organization.

Required fields:

- `TenantId`
- `Name`
- `Slug`
- `DefaultCulture`
- `TimeZone`
- `CreatedAt`
- `Status`

Requirements:

- All platform data must be tenant-aware.
- Tenants must be isolated at the data, permission, search, vector, and analytics levels.

---

### 5.2 Site

Represents a customer intranet or portal.

Required fields:

- `SiteId`
- `TenantId`
- `Name`
- `Slug`
- `Theme`
- `NavigationConfig`
- `HomePageConfig`

Examples:

- Corporate Intranet
- Factory Portal
- HR Portal
- Maintenance Portal
- Department Workspace

---

### 5.3 Container

A container is the main abstraction for grouping content.

Examples:

- Newsfeed
- Forum
- File Library
- Directory
- Project Space
- Department Space
- Incident Register
- Knowledge Base
- Business Entity Collection

Required fields:

- `ContainerId`
- `TenantId`
- `SiteId`
- `Type`
- `Name`
- `Title`
- `Description`
- `RendererKey`
- `SettingsJson`
- `PermissionsPolicyId`
- `IsSystemGenerated`
- `CreatedAt`
- `UpdatedAt`

Example container types:

```text
feed
forum
files
entity-list
directory
dashboard
system-feed
knowledge-base
```

Requirements:

- Containers must be generic.
- Business meaning should be added through configuration, metadata schema, labels, relationships, and renderers.
- Containers may be user-created or system-generated.
- Containers must support permissions and AI configuration.

---

### 5.4 Content

Content is the primary business record inside a container.

Examples:

- Outage
- Announcement
- Forum Thread
- Document
- Policy
- Machine
- Employee Profile
- Customer Complaint
- Maintenance Request

Required fields:

- `ContentId`
- `TenantId`
- `ContainerId`
- `ContentType`
- `Name`
- `Title`
- `Body`
- `Status`
- `AuthorUserId`
- `CreatedAt`
- `UpdatedAt`
- `PublishedAt`
- `MetadataJson`
- `SearchText`

Requirements:

- Content must support flexible metadata.
- Content must support labels.
- Content must support comments, optionally.
- Content must support posts, optionally.
- Content must support file attachments, optionally.
- Content must support AI indexing.
- Content must support audit history.

---

### 5.5 Post

A post represents an update, message, activity, event, comment-like record, or child item attached to content or directly to a container.

Examples:

- Feed post
- Forum reply
- Outage update
- Status update
- System event
- File activity
- Comment
- Timeline entry

Required fields:

- `PostId`
- `TenantId`
- `ContainerId`
- `ContentId` nullable
- `PostType`
- `Title`
- `Body`
- `AuthorUserId` nullable for system posts
- `IsSystemGenerated`
- `CreatedAt`
- `MetadataJson`

Requirements:

- Posts must support metadata independently from parent content.
- Posts must be independently indexable for search and AI.
- Posts must be usable as timeline/activity records.
- Posts must be usable as business events.

---

### 5.6 Label

Labels are generic tags that can be attached to containers, content, posts, files, entities, or users.

Required fields:

- `LabelId`
- `TenantId`
- `Name`
- `Slug`
- `Color`
- `Description`

Requirements:

- Labels should support hashtag-like usage.
- Labels should be queryable.
- Labels should power filtering, recommendations, and analytics.
- AI should be able to suggest labels automatically.

---

### 5.7 Metadata

Metadata is the core intelligence layer of the platform.

Metadata can exist on:

- Container
- Content
- Post
- File
- User
- Entity
- Relationship

Examples for a power outage use case:

```json
{
  "durationSeconds": 5400,
  "location": "Factory A",
  "machineId": "Generator-3",
  "shift": "Night",
  "severity": "High",
  "cause": "Voltage fluctuation",
  "startedAt": "2026-06-10T02:15:00Z",
  "endedAt": "2026-06-10T03:45:00Z"
}
```

Requirements:

- Metadata must be schema-driven but flexible.
- Metadata fields must support data types.
- Metadata must be queryable.
- Metadata must be aggregatable.
- Metadata must be permission-aware.
- Metadata must be available to analytics and AI pipelines.
- Metadata must support business interpretation through named metrics.

Recommended metadata field types:

```text
string
number
integer
boolean
date
datetime
duration
user-reference
content-reference
container-reference
choice
multi-choice
geo-location
json
```

---

### 5.8 Metadata Schema

Each container or content type may define a metadata schema.

Example:

```json
{
  "contentType": "Outage",
  "fields": [
    {
      "name": "durationSeconds",
      "type": "integer",
      "label": "Duration in Seconds",
      "required": true,
      "aggregations": ["sum", "avg", "min", "max"]
    },
    {
      "name": "machineId",
      "type": "content-reference",
      "label": "Machine",
      "targetType": "Machine"
    },
    {
      "name": "severity",
      "type": "choice",
      "values": ["Low", "Medium", "High", "Critical"]
    }
  ]
}
```

Requirements:

- Schemas must be versioned.
- Schema changes must not break existing content.
- Metadata validation should be enforced at API and UI levels.
- Schema fields should drive forms, filters, analytics, and AI prompt context.

---

## 6. Generic Metrics and Business Interpretation

The platform should provide generic metrics over containers, content, posts, comments, labels, and metadata.

Generic metrics:

```text
container.content.count
container.post.count
container.comment.count
container.label.count
content.post.count
metadata.sum(field)
metadata.avg(field)
metadata.min(field)
metadata.max(field)
metadata.countBy(field)
metadata.groupBy(field)
metadata.distinctCount(field)
```

Business metrics are named interpretations of generic metrics.

Example:

```json
{
  "metricName": "TotalOutageTimeLast7Days",
  "title": "Total Outage Time in Last 7 Days",
  "sourceContainer": "PowerOutages",
  "filter": "createdAt >= today - 7d",
  "aggregation": "sum",
  "field": "durationSeconds",
  "unit": "seconds",
  "displayUnit": "hours"
}
```

Generic result:

```text
metadata.sum(durationSeconds) = 51120
```

Business interpretation:

```text
Factory lost 14.2 hours of production due to power outages in the last 7 days.
```

Requirements:

- Metrics must be definable by configuration.
- Metrics must support filters.
- Metrics must support time windows.
- Metrics must support grouping.
- Metrics must support unit conversion.
- Metrics must support API access.
- Metrics must be usable in dashboards and AI-generated insights.

---

## 7. System-Generated Containers and Views

The platform should support containers that are generated from filters, rules, or queries.

Example:

```html
<iq-feed id="Last7DaysOutages"
         title="Last 7 Days Outages"
         source="PowerOutages"
         filter="createdAt >= today - 7d"
         system-generated="true" />
```

Requirements:

- System-generated containers should behave like normal containers in UI.
- They should not duplicate underlying data unless explicitly configured.
- They should expose their own metrics.
- They should support AI summaries and chat.
- They should support permission inheritance from source containers.

Examples:

- Last 7 Days Outages
- Open Maintenance Issues
- Critical Customer Complaints
- Recently Updated Policies
- Trending Discussions
- My Department Updates

---

## 8. AI Requirements

AI should not be a bolt-on feature. AI should be a platform capability available to every container, content item, post, file, and metadata record.

### 8.1 Vectorization

Requirements:

- Content and posts should be converted into embeddings.
- Files should be extracted, chunked, and embedded.
- Metadata should be included in AI context.
- Embeddings must preserve links back to source records.
- Embeddings must respect tenant and permission boundaries.
- Vector indexing should run asynchronously.
- Content updates should trigger re-indexing.

Recommended embedding payload:

```json
{
  "tenantId": "tenant-001",
  "sourceType": "content",
  "sourceId": "content-123",
  "containerId": "PowerOutages",
  "title": "Generator 3 outage",
  "chunkText": "Generator 3 was down for 90 minutes...",
  "metadata": {
    "durationSeconds": 5400,
    "location": "Factory A",
    "severity": "High"
  },
  "permissions": ["MaintenanceTeam", "FactoryManagers"]
}
```

---

### 8.2 Chat with Content

Users should be able to chat with:

- A container
- A system-generated view
- A content item
- A file library
- A forum
- A feed
- A business entity
- The whole intranet, subject to permissions

Example questions:

```text
What caused the most outages last week?
Summarize unresolved maintenance issues.
Which machines are repeatedly mentioned in outage reports?
What changed in the HR policies this month?
Show me safety documents related to emergency shutdown.
```

Requirements:

- AI chat must use retrieval-augmented generation.
- AI answers must cite source content where possible.
- AI must respect permissions.
- AI should expose structured follow-up suggestions.
- AI should be able to query metrics as tools/functions.

---

### 8.3 Summaries and Digests

Supported summary types:

- Daily feed summary
- Weekly department summary
- Content item summary
- Thread summary
- File summary
- Executive summary
- Incident summary
- Unresolved issues summary

Example:

```html
<iq-newsfeed id="PowerOutages"
             title="Power Outages"
             ai-summary="daily" />
```

Requirements:

- Summaries should be generated on schedule or on demand.
- Summaries should be stored as system posts or AI artifacts.
- Summaries should cite source records.
- Summaries should be regeneratable.

---

### 8.4 Insight Generation

Insights should be generated from:

- Content
- Posts
- Metadata
- Labels
- Relationships
- Time-series activity
- User interactions
- Metrics

Examples:

```text
Power outages increased by 32% this week compared to last week.
Generator 3 appears in 41% of high-severity outage reports.
Night shift has the highest average outage duration.
Three unresolved outage threads mention missing spare parts.
Policy documents about emergency shutdown have not been updated in 18 months.
```

Requirements:

- Insights must be explainable.
- Insights must link to underlying metrics and source content.
- Insights should be stored as structured records.
- Insights should support severity, confidence, category, and recommendation fields.
- Insights should be dismissible, acknowledged, or converted into tasks.

Suggested insight schema:

```json
{
  "insightId": "insight-001",
  "tenantId": "tenant-001",
  "containerId": "PowerOutages",
  "title": "Generator 3 is linked to repeated outages",
  "summary": "Generator 3 appears in 41% of high-severity outage reports over the last 30 days.",
  "category": "Operational Risk",
  "severity": "High",
  "confidence": 0.86,
  "metricRefs": ["HighSeverityOutagesByMachine"],
  "sourceRefs": ["content-123", "content-456"],
  "recommendation": "Inspect Generator 3 voltage stabilization components."
}
```

---

## 9. Declarative Component Requirements

The platform should provide Razor Tag Helpers for rendering and binding platform components.

Candidate Tag Helpers:

```html
<iq-container />
<iq-newsfeed />
<iq-forum />
<iq-files />
<iq-directory />
<iq-entity />
<iq-comments />
<iq-labels />
<iq-metric />
<iq-dashboard />
<iq-ai-chat />
<iq-ai-summary />
<iq-insights />
```

Example:

```html
<iq-dashboard id="FactoryOperations"
              title="Factory Operations Dashboard">
    <iq-metric name="TotalOutageTimeLast7Days" />
    <iq-metric name="OutageCountLast7Days" />
    <iq-feed id="Last7DaysOutages" />
    <iq-insights source="PowerOutages" />
</iq-dashboard>
```

Requirements:

- Tag Helpers should be declarative.
- Tag Helpers should map to platform configuration.
- Tag Helpers should be able to auto-create missing configuration in development mode.
- Production mode should require explicit migration/registration.
- Components should support theming and customization.
- Components should expose extensibility points for custom renderers.

---

## 10. APIs

The platform must expose APIs for external systems.

### 10.1 Content API

Required operations:

```text
Create content
Update content
Delete/archive content
Get content
Search content
Attach labels
Update metadata
Add relationship
```

### 10.2 Post API

Required operations:

```text
Create post
Create system post
Update post
Delete/archive post
Get posts by container
Get posts by content
Update post metadata
```

### 10.3 Metadata API

Required operations:

```text
Set metadata field
Bulk update metadata
Validate metadata against schema
Query metadata
Aggregate metadata
```

### 10.4 Metrics API

Required operations:

```text
Get metric value
Get metric history
Compare metric across time windows
Group metric by metadata field
Create named metric
Update named metric
```

### 10.5 AI API

Required operations:

```text
Chat with container
Chat with content
Chat with file library
Generate summary
Generate insights
Re-index content
Search semantically
Explain metric
```

---

## 11. Permission and Security Requirements

Requirements:

- Tenant isolation is mandatory.
- All content, posts, files, metadata, vectors, metrics, and insights must be permission-aware.
- AI retrieval must only retrieve content the user is allowed to access.
- Metrics must not leak restricted records.
- System-generated views must inherit or enforce source permissions.
- Audit logs must track sensitive actions.
- API access must support service accounts and scoped permissions.
- External system metadata updates must be auditable.

Recommended permission model:

```text
Tenant
Site
Container
Content
Post
Field-level metadata, optional
```

Supported actions:

```text
view
create
edit
delete
comment
label
manage-permissions
manage-schema
view-metrics
use-ai-chat
view-insights
admin
```

---

## 12. Search Requirements

Search should include:

- Keyword search
- Metadata filters
- Label filters
- Semantic/vector search
- Hybrid search
- Permission filtering
- Facets
- Date filters
- Container-level search
- Site-wide search

Search result types:

```text
Container
Content
Post
File
Comment
User
Entity
Insight
Metric
```

---

## 13. Analytics and Dashboard Requirements

Dashboard components should support:

- Metric cards
- Trend charts
- Grouped breakdowns
- Recent activity
- Top labels
- Most discussed content
- Unresolved items
- AI insights
- Natural language explanation of charts

Example dashboard declaration:

```html
<iq-dashboard id="PowerOutageDashboard"
              title="Power Outage Dashboard">
    <iq-metric name="OutageCountLast7Days" />
    <iq-metric name="TotalOutageTimeLast7Days" />
    <iq-chart metric="OutageCount" group-by="machineId" />
    <iq-insights source="PowerOutages" />
</iq-dashboard>
```

---

## 14. Suggested .NET Implementation Approach

### 14.1 Application Framework

Recommended stack:

```text
ASP.NET Core MVC
Razor Pages
Razor Views
Custom Tag Helpers
Entity Framework Core
ASP.NET Core Identity or external identity provider
Background workers
Minimal APIs or MVC APIs
```

Why this fits:

- Razor and Tag Helpers are a natural match for declarative intranet composition.
- ASP.NET Core is suitable for enterprise web applications.
- .NET has strong support for dependency injection, background services, authentication, authorization, and database integration.
- Custom Tag Helpers can map cleanly to platform components like feeds, forums, metrics, and AI chat.

---

### 14.2 AI and Vector Layer

Candidate .NET AI building blocks:

```text
Microsoft.Extensions.AI
Microsoft.Extensions.VectorData
Semantic Kernel, optional
ML.NET, optional
ONNX Runtime, optional
System.Numerics.Tensors / TensorPrimitives for lower-level numeric operations
```

Initial recommendation:

- Use `Microsoft.Extensions.AI` abstractions for chat and embeddings provider abstraction.
- Use `Microsoft.Extensions.VectorData` abstractions to avoid locking into one vector database too early.
- Use Semantic Kernel only where orchestration, tool calling, plugins, or agent workflows are useful.
- Use TensorPrimitives only for local numeric/vector operations when needed; do not prematurely build a custom vector database.

Possible vector stores:

```text
Azure AI Search
PostgreSQL + pgvector
Qdrant
Redis Vector Search
Elasticsearch / OpenSearch vector search
SQL Server vector support if suitable in target environment
```

---

### 14.3 Database Approach

Recommended initial relational model:

```text
Tenants
Sites
Containers
ContentItems
Posts
Labels
ContentLabels
PostLabels
MetadataSchemas
MetadataFieldDefinitions
Comments
Files
Relationships
Metrics
MetricDefinitions
Insights
AuditLogs
AiEmbeddingsIndexReferences
```

Metadata storage options:

1. JSON column for flexibility.
2. Extracted indexed columns for frequently queried fields.
3. Separate metadata key-value table for advanced querying.
4. Hybrid approach recommended.

Recommended approach:

- Store flexible metadata in JSON.
- Promote important fields into typed/indexed columns or generated columns.
- Maintain schema definitions separately.
- Use background jobs to calculate metrics and materialized views for performance.

---

## 15. MVP Scope

The first MVP should prove the core abstraction and AI value without trying to rebuild all of SharePoint.

### MVP Components

1. Tenant and site setup.
2. Container model.
3. Content model.
4. Post model.
5. Labels.
6. Flexible metadata.
7. Metadata schema definition.
8. Feed component.
9. Forum/thread component.
10. Basic file library component.
11. Comments.
12. Named metrics.
13. Dashboard metric cards.
14. Semantic search.
15. Chat with a container.
16. AI summary for a feed or forum.
17. Basic insight generation from metadata.
18. Razor Tag Helpers for feed, forum, files, metric, dashboard, and AI chat.

### MVP Demo Scenario

Use case: Factory Power Outages

Components:

```html
<iq-newsfeed id="PowerOutages"
             title="Power Outages in Factory"
             allow-comments="true"
             ai-enabled="true" />

<iq-feed id="Last7DaysOutages"
         title="Last 7 Days Outages"
         source="PowerOutages"
         filter="createdAt >= today - 7d"
         system-generated="true" />

<iq-dashboard id="PowerOutageDashboard"
              title="Power Outage Dashboard">
    <iq-metric name="OutageCountLast7Days" />
    <iq-metric name="TotalOutageTimeLast7Days" />
    <iq-ai-chat source="PowerOutages" />
    <iq-insights source="PowerOutages" />
</iq-dashboard>
```

External system posts outage records through API:

```json
{
  "containerId": "PowerOutages",
  "contentType": "Outage",
  "title": "Generator 3 outage",
  "body": "Generator 3 lost power during night shift.",
  "metadata": {
    "durationSeconds": 5400,
    "machineId": "Generator-3",
    "location": "Factory A",
    "shift": "Night",
    "severity": "High"
  },
  "labels": ["power", "generator", "factory-a"]
}
```

Expected MVP insights:

```text
There were 12 outages in the last 7 days.
Total outage duration was 14.2 hours.
Generator 3 was involved in 41% of high-severity outages.
Night shift had the highest average outage duration.
```

---

## 16. Non-Functional Requirements

### Performance

- Container pages should render quickly under typical intranet load.
- Metrics should use caching/materialization where needed.
- AI indexing should run asynchronously.
- Large file extraction and embedding should not block user requests.

### Scalability

- Multi-tenant architecture from day one.
- Background jobs for indexing, summarization, metrics, and insights.
- Separate vector storage from primary relational database if needed.

### Reliability

- AI failures must not break normal intranet functionality.
- Indexing failures should be retryable.
- API operations should be idempotent where possible.

### Auditability

- Track creation and modification of containers, content, posts, metadata, schemas, metrics, and insights.
- Track external API writes.
- Track AI-generated artifacts and source references.

### Extensibility

- Custom container types.
- Custom renderers.
- Custom metadata field types.
- Custom metric providers.
- Custom AI providers.
- Custom vector stores.
- Custom business rules.

---

## 17. Open Design Questions for Engineering Brainstorming

1. Should metadata be stored primarily as JSON, key-value rows, generated columns, or a hybrid?
2. Should Tag Helpers auto-provision containers in production, or only in development?
3. How should schema migrations work for metadata definitions?
4. Should system-generated containers be saved entities or virtual query views?
5. How should permission filters be applied to vector search results?
6. How should metrics avoid leaking restricted content counts?
7. Should AI summaries be stored as posts, content, or separate AI artifacts?
8. How should external systems authenticate and post business metadata?
9. Which vector store should be used for MVP?
10. What is the minimum generic renderer system needed for feeds, forums, files, and dashboards?
11. Should business entities be content items, separate records, or both?
12. How should relationships between entities, posts, files, and labels be modeled?
13. Should insights be generated on a schedule, on demand, or both?
14. How much of the platform should be configurable through admin UI versus code declarations?
15. Should customers get isolated databases, shared database with tenant isolation, or both deployment options?

---

## 18. Implementation Milestones

### Milestone 1: Core Platform Primitives

- Tenant
- Site
- Container
- Content
- Post
- Label
- Metadata
- Comments
- Basic permissions

### Milestone 2: Declarative Components

- Razor Tag Helpers
- Feed renderer
- Forum renderer
- File library renderer
- Metric card renderer
- Dashboard renderer

### Milestone 3: Metadata and Metrics

- Metadata schemas
- Metadata validation
- Named metrics
- Aggregations
- Dashboard cards
- System-generated views

### Milestone 4: AI Foundation

- Text extraction
- Chunking
- Embeddings
- Vector store integration
- Semantic search
- Chat with container
- AI summaries

### Milestone 5: Insights

- Scheduled insight jobs
- Metric-driven insight templates
- AI-generated explanations
- Insight cards
- Source references
- Acknowledge/dismiss workflow

### Milestone 6: Vendor/Customer Readiness

- Admin UI
- Theming
- Deployment templates
- API keys/service accounts
- Audit logs
- Documentation
- Sample intranet templates

---

## 19. Core Product Principle

The core design principle should be:

> Everything is content. Everything has metadata. Everything can be measured. Everything can be understood by AI.

A more technical version:

> WorkplaceIQ is built on abstract content primitives, enriched by metadata, connected by relationships, exposed through reusable renderers, and activated by AI.

