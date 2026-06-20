# ADR-Storage-PgVector-Connector-01: SK PgVector Connector & Npgsql Version Compatibility

---

## Context

WorkplaceIQ supports multiple storage backends: SQLite (default dev), PostgreSQL with pgvector, SQL Server, and InMemory.

The `Microsoft.SemanticKernel.Connectors.PgVector` NuGet package (referenced from `WorkplaceIQ.Web.csproj`) provides a `VectorStore` implementation backed by PostgreSQL with the pgvector extension. The project also references `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x for the EF Core relational store.

In CodeMemory's experience (see [CodeMemory ADR Library-Storage-PgVector-01](https://github.com/khurram-uworx/CodeMemory/blob/main/docs/adr/Library-Storage-PgVector-01.md)), this combination can cause a runtime `MissingMethodException`:

```
System.MissingMethodException: Method not found:
  'System.Threading.Tasks.Task Npgsql.NpgsqlConnection.ReloadTypesAsync()'
```

The SK PgVector connector (older versions) called `NpgsqlConnection.ReloadTypesAsync()` — an API removed in Npgsql 10.x (replaced by `NpgsqlDataSource.ReloadTypes()`). Since `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x transitively resolves Npgsql 10.x, an assembly unification at runtime provides the 10.x version — but the method no longer exists there.

---

## Decision

**Reference `Microsoft.SemanticKernel.Connectors.PgVector` in the web host project and attempt to use its `PostgresVectorStore` class at runtime.**

The upstream fix ([microsoft/semantic-kernel#13724](https://github.com/microsoft/semantic-kernel/pull/13724)) has been shipped in the 1.74.0-preview release. The current NuGet resolution produces Npgsql 10.x with no assembly conflicts, indicating the connector has been updated to target the Npgsql 10.x API surface.

If a `MissingMethodException` is observed at runtime with pgvector, the resolution path is:

1. Verify the installed `Microsoft.SemanticKernel.Connectors.PgVector` version supports Npgsql 10.x
2. If not, pin to an older `Npgsql.EntityFrameworkCore.PostgreSQL` (8.x) — invasive, requires EF Core downgrade
3. Fall back to a custom `PgVectorStore` implementation (copy from CodeMemory's `CodeMemory.AspNet.Storage.PgVector`)

---

## Rationale

### 1. SK connector provides full VectorStore abstraction

WorkplaceIQ uses `VectorStore` (from `Microsoft.Extensions.VectorData`) for the SignalFlow pipeline's classification vector storage. Using the SK connector keeps the implementation consistent with the `InMemoryVectorStore`, `SqliteVectorStore`, and `SqlServerVectorStore` used by other providers.

### 2. Single implementation to maintain

A custom PgVectorStore would add ~300 lines of ADO.NET SQL generation, type mapping, and error handling — code that already exists and is tested in the SK ecosystem.

### 3. Npgsql version resolution confirmed

Current build output resolves to Npgsql 10.0.3 with no version conflicts, suggesting the SK connector 1.74.0-preview is compatible.

---

## Consequences

### Positive

- Consistent VectorStore abstraction across all four providers
- No custom PostgreSQL vector store code to maintain
- DI-friendly via `services.AddPostgresVectorStore(connectionString)` pattern
- The existing `dotnet list --include-transitive` shows only Npgsql 10.x (no 8.x conflict)

### Negative

- If a future SK PgVector connector release reintroduces the Npgsql 8.x dependency, the runtime error may reappear
- The SK connector's API surface is less flexible than a custom implementation (schema isolation, index tuning)

### Neutral

- The `Microsoft.SemanticKernel.Connectors.PgVector` package adds ~200 KB to the deployment
- `Npgsql` and `Pgvector` NuGet packages remain as direct dependencies regardless

---

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| **Do not support pgvector vector store** | Inconsistent with the existing EF Core pgvector support; leaves a gap in the SignalFlow pipeline |
| **Custom PgVectorStore (CodeMemory approach)** | Adds maintenance burden; viable fallback if the SK connector fails at runtime |
| **Pin Npgsql to 8.0.7** | CS1705: EF Core provider compiled against Npgsql 10.x — assembly version mismatch |
| **Remove SK PgVector connector** | Breaks the provider-consistent vector store pattern; forces pgvector users to inmemory vector store |
| **Wait for stable SK release** | Preview is available now and builds cleanly; waiting indefinitely blocks pgvector vector store support |
