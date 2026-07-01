# WorkplaceIQ

A metadata-driven content platform for building intranets, knowledge hubs, and operational portals using abstract primitives — containers, content items, labels, and relationships.

## Quick Start

```powershell
# Local (SQLite)
dotnet run --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj

# Docker (with MinIO file storage)
docker compose up --build
```

App: `http://localhost:4792`. MinIO: `http://localhost:9000` / console at `:9001` (`workplaceiq` / `workplaceiq-secret`).

## Projects

| Project | Purpose |
|---------|---------|
| `WorkplaceIQ` | Core domain model, service interfaces, metric providers |
| `WorkplaceIQ.AspNet` | EF Core DbContext, Tag Helpers, HTML renderers, S3/MinIO storage |
| `WorkplaceIQ.Web` | Demo/reference app (SQLite, seeded data) |
| `WorkplaceIQ.ServiceDefaults` | OpenTelemetry, health checks, resilience |
| `WorkplaceIQ.AppHost` | .NET Aspire orchestrator (PostgreSQL + MinIO) |
| `WorkplaceIQ.Tests` | NUnit tests |

## Tag Helpers

| Tag | Renders |
|-----|---------|
| `<iq-feed>` | Activity stream with posts, labels, comments |
| `<iq-forum>` | Threaded discussions |
| `<iq-files>` | File library with upload/download (S3/MinIO) |
| `<iq-entity>` | Single business entity detail view |
| `<iq-entity-list>` | Entity directory with relationships |
| `<iq-metric>` | Configurable metric card (count / sum / avg / min / max) |

Prefix: `iq-`. Registered via `@addTagHelper *, WorkplaceIQ.AspNet`.

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

Container types: `DiscussionContent`, `FolderContent`, `FeedContent`, `GroupContent`.

## Storage Providers

Provider selection via `Storage:Provider` in `appsettings.json` (default: `sqlite`).

| Provider | EF Core | Vector Store | Connection String Key |
|----------|---------|-------------|----------------------|
| `sqlite` | `UseSqlite` | `SqliteVectorStore` | `ConnectionStrings:Sqlite` |
| `pgvector` | `UseNpgsql` | `PostgresVectorStore` | `ConnectionStrings:Npgsql` / `PgVector` |
| `sqlserver` | `UseSqlServer` | `SqlServerVectorStore` | `ConnectionStrings:SqlServer` |
| `inmemory` | `UseInMemoryDatabase` | `InMemoryVectorStore` | none |

See [ADR-01: SK PgVector Connector Compatibility](docs/adr/01-Library-Storage-PgVector-Connector.md) for known Npgsql version constraints.

## Architecture Decisions

Architecture Decision Records (ADRs) are stored in [`docs/adr/`](docs/adr/). Each ADR documents a significant design decision with context, rationale, and tradeoffs.

## Tech Stack

.NET 10 / ASP.NET Core MVC / EF Core 10 / SQLite (default) + PostgreSQL (optional) / S3 + MinIO / Bootstrap 5 / OpenTelemetry / NUnit 4 / Semantic Kernel VectorStore connectors.

## Build & Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```
