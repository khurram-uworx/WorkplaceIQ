# Agent Notes

Start by reading [README.md](README.md) for project overview, component inventory, quick start, and tech stack.

This repo is still early and in flux. Keep changes small, sequential, and easy to verify.

## Tools

- **code-memory** — Discover architecture, components, symbols, and their relationships. Use `semantic_search`, `trace_dependency`, `get_edit_context`, and `impact_analysis` to understand code before editing.
- **memori** — Store and recall facts, decisions, and context across sessions. Use `remember` to persist findings and `search` to retrieve them in future sessions.
- **microsoft-learn** — Check official .NET / ASP.NET / Azure documentation for modern idioms, patterns, and APIs. Use when generating code to ensure alignment with current Microsoft conventions.

## Local App

- Use the deterministic demo URL: `http://localhost:4792`.
- Launch the web app with:

```powershell
dotnet run --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj
```

- Do not invent or rotate ports unless `4792` is genuinely unavailable.
- For smoke checks, give the app enough time to finish seeding and binding before requesting the page.
- In this environment, `dotnet run --no-restore --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj` was the reliable way to keep the app up after restore/build had already succeeded.
- If you need a background local run for smoke testing, redirect stdout and stderr to files so the process stays alive long enough to serve requests. Reliable pattern:

  ```powershell
  # Start in background (does NOT block)
  $p = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj" -WindowStyle Hidden -RedirectStandardOutput "$env:TEMP\wiq-smoke.log" -RedirectStandardError "$env:TEMP\wiq-smoke-err.log" -PassThru
  Write-Host "PID: $($p.Id)"

  # Wait for startup, then test
  Start-Sleep -Seconds 10
  Invoke-WebRequest -Uri "http://localhost:4792/" -UseBasicParsing

  # Clean up
  Stop-Process -Id $p.Id -Force
  ```

## Build And Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

The repo may use the latest installed .NET SDK, including preview SDKs, while projects still target `net10.0`.

## Shell Tools

- Linux-style command-line tools are available directly from PowerShell and can be used when they make debugging faster, including `rg`, `grep`, `awk`, `sed`, and related coreutils.
- Do not wrap commands in `bash`; WSL may not have a distro installed. Call the available tools directly.

## Conventions

- Public Tag Helper prefix is `iq-`, for example `<iq-feed id="CompanyNews" title="News Feed" />`.
- Keep tests split by behavior instead of adding everything to one large file.

## Domain Boundaries

WorkplaceIQ is a metadata-driven content platform — containers, content items, labels, and relationships. It is NOT a generic CMS, a chat UI, or a data pipeline framework.

Forbidden: custom LLM clients, custom embedding pipelines, custom DI containers, custom vector databases.

## ADR Style

ADRs (Architecture Decision Records) live in `docs/adr/` and follow the format: Context → Decision → Rationale → Consequences → Alternatives considered.

ADRs should describe **what** the system does at an architectural level and **why**, without pinning down implementation method names, parameter types, or class signatures that can drift during implementation. Include intent, constraints, and tradeoffs; leave concrete API surface to the code. If an ADR contradicts the implementation, update the ADR toward the abstract intent — the implementation is the source of truth for specifics.

### Existing ADRs

| ADR | Topic | Status |
|-----|-------|--------|
| [01-Library-Storage-PgVector-Connector](docs/adr/01-Library-Storage-PgVector-Connector.md) | SK PgVector connector & Npgsql version compatibility | Landed |
| [02-Domain-Content-Modeling](docs/adr/02-Domain-Content-Modeling.md) | Unified polymorphic content model (TPT containers, standalone ContentItem) | Landed |
| [03-ADR-UI-DualLayer](docs/adr/03-ADR-UI-DualLayer.md) | Dual UI Layer — Tag Helpers & Dedicated Controllers | Next |
| [04-Metrics-Platform](docs/adr/04-Metrics-Platform.md) | OpenTelemetry-driven metrics platform | Future |

## DI Conventions

- `WorkplaceIQ.AspNet/ServiceCollectionExtensions.cs` is the central DI registration point.
- Per-provider extension methods (`AddWorkplaceIqSqliteStorage`, `AddWorkplaceIqSqlServerStorage`, etc.) call the generic `AddWorkplaceIqAspNet` internally.
- `IWorkplaceIqStore` → `EfWorkplaceIqStore` uses `IDbContextFactory<WorkplaceIqDbContext>` for thread safety.
- All service classes depend on `IWorkplaceIqStore`, never on `WorkplaceIqDbContext` directly.
- Prefer `AddSingleton` for stateless services, `AddScoped` for stateful services that depend on EF Core.

## Storage Providers

Provider selection via `Storage:Provider` in `appsettings.json`:
- `sqlite` (default dev) — `UseSqlite` + `SqliteVectorStore`
- `pgvector` — `UseNpgsql` + `PostgresVectorStore`
- `sqlserver` — `UseSqlServer` + `SqlServerVectorStore`
- `inmemory` — `UseInMemoryDatabase` + `InMemoryVectorStore`

See [ADR-01 Library-Storage-PgVector-Connector](docs/adr/01-Library-Storage-PgVector-Connector.md) for known compatibility notes on the PgVector/SK connector.

## Code Style

- **Primary constructors:** Preferred for service/DI classes
- **`sealed class`:** Default for non-abstract classes
- **Collection expressions:** `[]` for empty/static, `new List<T>()` for mutable
- **Entity configurations:** Use `IEntityTypeConfiguration<T>` classes in `WorkplaceIQ.AspNet/Data/Configurations/` and apply via `modelBuilder.ApplyConfiguration()` in `OnModelCreating`

## Project Structure

```
src/
├── WorkplaceIQ/            # Domain model, interfaces, service abstractions
├── WorkplaceIQ.AspNet/     # EF Core DbContext, store impl, tag helpers, renderers
│   └── Data/
│       ├── Configurations/ # IEntityTypeConfiguration<T> classes
│       ├── WorkplaceIqDbContext.cs
│       └── EfWorkplaceIqStore.cs
├── WorkplaceIQ.Web/        # Web host (Program.cs, controllers, views, SignalFlow)
├── WorkplaceIQ.ServiceDefaults/  # OpenTelemetry, resilience
└── WorkplaceIQ.AppHost/    # .NET Aspire orchestrator
```

## Common Pitfalls

- `EfWorkplaceIqStore` uses `IDbContextFactory<WorkplaceIqDbContext>` — do not inject `WorkplaceIqDbContext` directly into services.
- The SK PgVector connector (`Microsoft.SemanticKernel.Connectors.PgVector`) may have Npgsql version compatibility issues at runtime — see [ADR](docs/adr/01-Library-Storage-PgVector-Connector.md).

## Testing

- **Framework:** NUnit 4.x
- **Test doubles:** `InMemoryWorkplaceIqStore` (list-based), `InMemoryFileObjectStorage`
- **Naming:** `Method_Scenario_ExpectedBehavior`
- **Pattern:** Arrange-Act-Assert (no comments needed)
