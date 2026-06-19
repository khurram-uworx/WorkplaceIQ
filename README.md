# WorkplaceIQ

A metadata-driven content platform for building intranets, knowledge hubs, and operational portals using abstract primitives — containers, content, posts, labels, metadata, and relationships.

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

## Core Model

```
Container → Content → Post
                ├── Metadata (JSON)
                ├── Labels (many-to-many)
                ├── Comments (Post with Comment type)
                └── Relationships (directed, with metadata)
```

Container types: `FeedContainer`, `ForumContainer`, `FileContainer`, `EntityContainer`, `Directory`, `Dashboard`, `SystemFeed`, `KnowledgeBase`.

## Tech Stack

.NET 10 / ASP.NET Core MVC / EF Core 10 / SQLite (default) + PostgreSQL (optional) / S3 + MinIO / Bootstrap 5 / OpenTelemetry / NUnit 4.

## Build & Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```
