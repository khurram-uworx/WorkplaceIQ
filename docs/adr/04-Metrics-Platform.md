# ADR-Metrics-Platform-04: OpenTelemetry-Driven Metrics Platform

---

## Context

WorkplaceIQ's existing metrics infrastructure spans multiple layers:

- **ADR 02** defined the core content model (Containers, ContentItems, Labels). This ADR extends it with `ContentMetrics` and `ContentItemMetrics` link tables for persisting metric data alongside entities.
- **ADR 03** defined the `<iq-metric>` tag helper and well-known URL conventions.
- **Code** has `IMetricProvider` / `IMetricService` / `MetricService` with `System.Diagnostics.Metrics` instrumentation (`Meter("WorkplaceIQ")`), `ContentCountMetricProvider`, `MetadataAggregationMetricProvider` (sum/avg/min/max), and `MetricTagHelper`.
- **OpenTelemetry** is already wired in `ServiceDefaults` (AspNetCore/HttpClient/Runtime instrumentation) and `Program.cs` (`AddMeter("WorkplaceIQ")`).
- **REQUIREMENTS.md §6** describes generic metrics (`container.content.count`, `metadata.sum(field)`, etc.) and business metrics as named interpretations.

**Three gaps remain:**

1. **No category distinction** — The platform treats all metrics the same, but there are two fundamentally different kinds: (a) **computed** — ephemeral aggregations over CMS data computed on demand, and (b) **stored** — numeric measurements pushed by external systems (power outage duration, voltage levels, memory usage) that must be persisted in the DB.
2. **`MetricService` creates instruments per computation** — `RecordToInstrument` calls `_meter.CreateObservableGauge(...)` inside `ComputeSeriesAsync`, spawning a new OTel instrument on every request. The correct pattern is to create instruments once at startup with observable callbacks that perform computation on scrape.
3. **No programmatic/scrape-based metric URLs** — ADR 03 defined tag helpers and controller URLs for entity navigation but not for metric exposition. External systems (Prometheus, Grafana) need a standard endpoint to scrape metrics from the CMS.

---

## Decision

**Adopt a dual-category, dual-exposure metrics platform** built on OpenTelemetry as the foundation.

### 1. Two Metric Categories

| Category | Source | Persistence | Examples |
|---|---|---|---|
| **Computed** | CMS data queries via `IMetricProvider` | Ephemeral — computed on scrape/request only | `workplaceiq.container.content.count`, `workplaceiq.metadata.sum` |
| **Stored** | External system push via content API | Persisted in `ContentMetrics`/`ContentItemMetrics` tables | Power outage duration, voltage drop, memory spike |

Computed metrics use the existing `IMetricProvider` pattern unchanged — they remain always on-demand, never pre-computed. Stored metrics are new: they are submitted alongside content and persisted for later retrieval.

### 2. API Shape for External / Stored Metrics

External systems submit metrics inline during content creation. The content creation API gains an optional `metrics` parameter:

```
CreateFeedItem(
    containerId,
    title,
    body,
    metrics: [                       // optional
        { name: "outage.duration", value: 900, unit: "seconds" },
        { name: "voltage.drop", value: 150, unit: "volts" }
    ],
    ...)
```

Metrics are stored as rows in `ContentMetrics` / `ContentItemMetrics` (as designed in this ADR), linked to the parent Content or ContentItem. A `MetricDefinition` record in the DB defines the interpretation (name, unit, display unit, description, aggregation hint). If no definition exists for a submitted metric name, the platform creates one on the fly with defaults.

In the future a dedicated `/api/metrics` endpoint may be added for batch/decoupled submission, but inline is the primary pattern.

### 3. Dual Exposure Path

Both computed and stored metrics are exposed via two independent paths.

#### Path A: OpenTelemetry Standard `/metrics` Endpoint

The standard OTel Prometheus scraping endpoint (`/metrics` via `OpenTelemetry.Exporter.Prometheus.AspNetCore`) exposes all active metrics.

- **Computed metrics**: An `IMetricProvider`-backed `Meter` registers `ObservableGauge` instruments at startup. On each Prometheus scrape, the OTel SDK invokes the observable callbacks, which query the CMS data and return current values. No pre-computation, no storage.
- **Stored metrics**: A parallel `Meter` registers `ObservableGauge` instruments that query `ContentMetrics` tables on scrape, aggregating stored values per metric name.

Both meters use `IMeterFactory` (the ASP.NET Core recommended pattern) instead of `new Meter(...)`.

```
Prometheus → GET /metrics → OTel SDK → ObservableGauge callbacks → IMetricProvider / DB → Prometheus text
```

#### Path B: CMS Well-Known Metric URLs

Entity-scoped endpoints returning Prometheus exposition format directly from CMS queries, following the ADR 03 URL convention:

| URL | Scope | Returns |
|---|---|---|
| `/Feeds/{businessId}/Metrics` | Metrics for a single Feed container | All computed + stored metrics scoped to that feed, with `container.id` / `container.name` tags |
| `/Forums/{businessId}/Metrics` | Metrics for a single Discussion container | Same pattern |
| `/Folders/{businessId}/Metrics` | Metrics for a single FileFolder container | Same pattern |
| `/System/Metrics` | All metrics across all content types | Global rollup — every metric for every container, tagged with `container.type` and `container.id` |

These are ASP.NET Core controller actions (or minimal API endpoints) that:
1. Resolve the container / entity scope
2. Call `IMetricService` for computed metrics (on-demand)
3. Query `ContentMetrics` for stored metrics (DB read)
4. Format the combined result as Prometheus exposition text (`text/plain; version=0.0.4`)

The `/System/Metrics` endpoint is the Prometheus scrape target for CMS-wide monitoring.

```
Prometheus → GET /System/Metrics → Controller → IMetricService + DB → Prometheus text
```

### 4. Refactoring: `IMeterFactory` + Static Instrument Registration

The current `MetricService` creates `new Meter("WorkplaceIQ")` and calls `CreateObservableGauge` inside `RecordToInstrument` (invoked per computation). This is incompatible with OTel best practices.

**New pattern:**

```
MetricService (singleton)
├── Uses IMeterFactory (from DI) to create a Meter at startup
├── Registers ObservableGauge instruments once, with static callbacks
└── On scrape:
    ├── For computed metrics: callback invokes IMetricProvider.ComputeSeriesAsync()
    └── For stored metrics: callback queries ContentMetrics table via IWorkplaceIqStore
```

This ensures:
- Instruments exist for the lifetime of the process, created once
- No instrument leak per request
- Values are only computed when scraped (truly on-demand)
- Testing uses `MetricCollector<T>` with isolated `IMeterFactory` scopes

### 5. Store-and-Forward Architecture

For stored (externally-pushed) metrics, the data flow is:

```
External system → API (CreateFeedItem with metrics) → ContentMetrics table (DB) → Path A (OTel scrape callback) + Path B (well-known URL query)
```

No background job or queue is required initially — metrics are written at content creation time and available for query/scrape immediately on the next read. A background exporter to forward stored metrics to an external OTel collector or Prometheus remote write endpoint can be added later as performance demand grows.

---

## Rationale

### 1. OTel is the .NET standard for metrics

`System.Diagnostics.Metrics` + OpenTelemetry is the official Microsoft observability stack, integrated into ASP.NET Core, `Microsoft.Extensions` hosting, and the .NET Aspire ecosystem. Building on this avoids a proprietary metrics layer.

### 2. Dual path serves two distinct consumers

**Path A** (`/metrics`) serves the standard Prometheus/OTel ecosystem — any Prometheus server, Grafana dashboard, or OTel collector can scrape the CMS. **Path B** (`/Feeds/1/Metrics`) serves CMS-specific consumers who need entity-scoped metrics — a dashboard page for a single feed, an AI insight scoped to a specific container, or an external system querying metrics for just one feed.

### 3. Inline API keeps metrics contextual

External metrics submitted alongside content creation are naturally scoped to that content item. A power outage duration belongs to the outage feed entry it describes. Separating metric submission from content creation would require explicit linking logic and risk orphaned metrics.

### 4. DB-first store-and-forward

Storing metrics in `ContentMetrics` tables means they survive process restarts, can be queried historically, and share the CMS permission model defined in ADR 02's Container scope. The OTel scrape callback simply reads from the same tables — no dual-write or sync problem.

### 5. `IMeterFactory` enables testability

ASP.NET Core recommends `IMeterFactory` over `new Meter()` because it isolates instruments per DI container scope, allowing `MetricCollector<T>` to capture only the measurements from a specific test without cross-test interference.

---

## Consequences

### Positive

- **Standard-compliant** — Prometheus can scrape `/metrics` out of the box. Grafana dashboards work immediately.
- **Entity-scoped metrics** — `/Feeds/1/Metrics` gives you exactly the metrics for feed 1, with proper tags.
- **No pre-computation** — Computed metrics are zero-cost until scraped. Stored metrics cost one DB write at content creation time.
- **Extensible** — New `IMetricProvider` implementations add computed metrics automatically. External systems push stored metrics via the content API without code changes.
- **Unified permission model** — Both paths respect the CMS access controls (defined in ADR 02's Container-level permission scope).
- **Testable** — `IMeterFactory` + `MetricCollector<T>` provide first-class testability for metrics instrumentation.

### Negative

- **Dual path maintenance** — Two code paths that serve the same data (OTel SDK vs. CMS controller) could diverge. Mitigated by sharing the same `IMetricService` + DB queries underneath.
- **Prometheus exposition format in controller** — Path B endpoints must format Prometheus text manually. No official .NET library for this; requires a lightweight text writer (~50 lines, reusable).
- **OTel Prometheus exporter is pre-release** — `OpenTelemetry.Exporter.Prometheus.AspNetCore` has been pre-release; the API (`MapPrometheusScrapingEndpoint`) may change.
- **Stored metric unbounded growth** — If external systems push high-frequency metrics, `ContentMetrics` table grows. Initial mitigation: application-level retention or TTL can be added later.

### Neutral

- **`IMeterFactory` refactoring** — `MetricService` changes from `new Meter()` to constructor-injected `IMeterFactory`. No behavioral change for consumers.
- **Dedicated metrics endpoint pattern** — Follows ADR 03's controller-per-entity convention but adds a `Metrics` action to each entity controller.

---

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| **OTel-only, no CMS well-known URLs** | No way to get entity-scoped metrics (`/Feeds/1/Metrics`) without hitting the global `/metrics` endpoint and filtering client-side |
| **CMS-only, no OTel standard endpoint** | Breaks Prometheus/OTel ecosystem compatibility; forces every consumer to use CMS-specific URLs and auth |
| **Push-only (statsd/collectd style)** | Adds a UDP dependency and background aggregator; CMS loses visibility into what metrics exist |
| **Background job to compute metrics** | Defeats the "always on-demand" requirement; computed metrics would be stale between job runs |
| **Store externally-pushed metrics in a separate DB** | Loses relational integrity with content; no cascading delete or permission sharing with the parent content item |
| **Pre-register all possible metric instruments in code** | Can't anticipate every metric name an external system might push; DB-driven metric discovery is more flexible |

---

## Related

- [ADR Domain-Content-Modeling-02](02-Domain-Content-Modeling.md) — Polymorphic content model; this ADR extends it with stored-metrics link tables
- [ADR UI-DualLayer-03](03-ADR-UI-DualLayer.md) — Tag helpers and well-known URL conventions that the Path B metric endpoints follow
- [REQUIREMENTS.md §6](../REQUIREMENTS.md) — Generic metrics and business interpretation requirements
- [REQUIREMENTS.md §10.4](../REQUIREMENTS.md) — Metrics API operations (get, history, compare, group)
