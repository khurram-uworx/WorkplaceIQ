# ADR-UI-DualLayer-03: Dual UI Layer — Tag Helpers and Dedicated Entity Controllers

---

## Context

WorkplaceIQ is a "SharePoint Lite" for small-to-mid enterprises (2–50 people) where M365 does not yet make economic sense. Its UI must deliver both an embeddable content-authoring experience and first-class navigation for well-known business entities.

ADR 02 established a polymorphic content model (`Content` → `Container` / `ContentItem`) with dual GUID+int identity. This unlocks a consistent URL scheme and a shared rendering infrastructure, but the UI layer itself remains unformalized:

- **Tag helpers** (`<iq-feed>`, `<iq-forum>`, `<iq-files>`, `<iq-entity>`, `<iq-entity-list>`, `<iq-metric>`) provide CMS embedding — any Razor page can compose content widgets with HTML-attribute configuration.
- **Controllers** exist ad-hoc (`HomeController`, `ContentController`, `FilesController`) with no consistent routing convention for entity navigation.
- Observations.md #9 ("Missing UI Primitives") documents the absence of pagination, empty states, real-time progress, and label display — all gaps in a unified UI framework.

The platform needs a formal dual-layer architecture: tag helpers as the composition "jelly" for custom pages, and dedicated controllers with well-known URLs for first-class entity browsing. Both layers must support the SharePoint-inspired entity set — Discussions (forums), Files, Feeds, and their child items — and grow as new Container types are added.

---

## Decision

**Adopt a dual UI architecture** with two complementary layers, both backed by the polymorphic content model from ADR 02.

### Layer 1: Tag Helpers — CMS Composition (the "Jelly")

Tag helpers are the embeddable CMS widget layer, inspired by SharePoint web parts. They let any Razor page surface content from any Container without writing controller actions or view logic.

**Existing tag helpers** (continued and expanded):

| Tag Helper | Purpose |
|---|---|
| `<iq-feed>` | Renders feed entries from a Feed Container |
| `<iq-forum>` | Renders forum threads from a Discussion Container |
| `<iq-files>` | Renders file listings from a FileFolder Container |
| `<iq-entity>` | Renders a single Content or ContentItem detail |
| `<iq-entity-list>` | Renders a paginated list of entities from a Container |
| `<iq-metric>` | Renders an aggregated metric value |

**Tag helpers must evolve** to support the expanding entity surface:

- Existing tag helpers (Feed, Forum, Files) should accept any Container discriminator, not just their hard-coded type — making them generic entity renderers that adapt to the content type at runtime.
- Label/classification display should be a first-class rendering concern. A label chip or list rendered by any tag helper must carry a link to the content's well-known URL (`/Feeds/1/Post/3`), so users can navigate from a classification dashboard directly to the source entity.
- Composition is the primary strength: a dashboard page can embed `<iq-entity-list type="Discussion" />` and `<iq-files container-id="..." />` side by side, mixing entity types freely.

**Future tag helpers** (identified in Observations.md #9, to be designed during implementation):

- `<iq-pager>` — consistent pagination across all entity lists
- `<iq-empty-state>` — reusable empty-state display with action prompts
- `<iq-progress-bar>` — real-time progress integration with SignalR

### Layer 2: Dedicated Controllers — First-Class Entity Pages

Each major Container type gets a dedicated MVC controller with well-known, predictable URLs. Controllers are established incrementally as the entity set matures.

**First wave:**

| Controller | Container Type | Base URL |
|---|---|---|
| `ForumsController` | Discussion | `/Forums/{businessId:int}` |
| `FilesController` | FileFolder | `/Folders/{businessId:int}` |
| `FeedsController` | Feed | `/Feeds/{businessId:int}` |

**Second wave** (future, not yet scheduled):

| Controller | Container Type | Base URL |
|---|---|---|
| `CalendarsController` | Calendar | `/Calendars/{businessId:int}` |
| `WikisController` | Wiki | `/Wikis/{businessId:int}` |
| `ProjectsController` | Project | `/Projects/{businessId:int}` |

### URL Convention

All well-known URLs follow the entity hierarchy using the friendly integer IDs from ADR 02:

```
# Container (top-level entity)
/{containerType}/{businessId}
  → /Forums/1
  → /Folders/2
  → /Feeds/3

# ContentItem (child entity within a Container)
/{containerType}/{businessId}/{itemType}/{itemBusinessId}
  → /Forums/1/Thread/5
  → /Folders/2/File/12
  → /Feeds/3/Entry/8

# Resource action on a ContentItem
/{containerType}/{businessId}/{itemType}/{itemBusinessId}/{action}
  → /Folders/2/File/12/Download
  → /Forums/1/Thread/5/Attachments

# Label-based navigation (redirects to the entity's canonical URL)
/Labels/{labelName}
  → /Labels/Urgent          (lists or redirects to classified items)
```

GUID-based routes are reserved for API-internal use and admin operations, not for end-user navigation.

### Label Display and Classification Navigation

Labels are a cross-cutting concern that bridges Layer 1 and Layer 2:

- **In tag helpers:** When rendering an entity (feed entry, forum post, file), any associated labels or classifications display as styled chips. Each chip carries a link to the entity's well-known URL — `/Feeds/3/Entry/8` — so a SignalFlow classification dashboard can navigate the user directly to the source content.
- **In dedicated controllers:** Entity detail pages render labels in a sidebar or header region. Clicking a label navigates to `/Labels/{labelName}` which shows all content carrying that label, linked to their canonical URLs.
- **Generic label rendering** is a shared concern: both layers use the same partial/tag helper for label display, ensuring visual and navigational consistency.

---

## Rationale

### 1. SharePoint-like flexibility without M365 complexity

Tag helpers give the "web part" experience — drop content widgets into any page, configure via attributes, compose freely. Dedicated controllers give the structured navigation and deep linking that users expect for well-known entity types. Together they cover the full spectrum from ad-hoc content pages to formal application UI.

### 2. URLs as identity, not implementation detail

Well-known URLs let content reference each other naturally. A feed entry can point to `/Folders/2/File/12/Download`; a classification result links back to `/Feeds/3/Entry/8`. This is how the web was designed — resources identified by URL, not by opaque IDs embedded in query strings.

### 3. Gradual adoption

Teams can start with tag helpers embedded in custom pages and graduate to dedicated controllers as their content model matures. The URL convention is stable from day one; only the controller implementation is additive.

### 4. Separation of concerns

Tag helpers own reusable rendering — how an entity looks when embedded. Controllers own page-level concerns — routing, SEO metadata, social preview, authorization, and full page lifecycle. They share the same rendering infrastructure underneath, so visual consistency is automatic.

### 5. Polymorphic rendering

Because ADR 02 unified all entities under `Content`/`ContentItem`, tag helpers can become generic: one `<iq-entity-list>` can render any Container type, one `<iq-entity>` can render any ContentItem. Type-specific rendering (forum thread vs. file vs. feed entry) is a view template concern, not a routing or controller concern.

---

## Consequences

### Positive

- **Predictable URL scheme** — Every entity has exactly one canonical URL, derivable from its type and business ID.
- **CMS + dedicated pages coexist** — No forced choice; both patterns are first-class and interoperable.
- **Tag helpers grow naturally** — Existing helpers extend to new entity types without new infrastructure. Observations.md #9 gaps are addressed incrementally.
- **Label navigation closes the SignalFlow gap** — Classification results link directly to source content, making the AI pipeline a navigation entry point, not a dead end.
- **Container-per-controller scaling** — New entity types add one controller file and one route entry. No monolithic routing bloat.

### Negative

- **Two rendering paths to maintain** — Tag helper rendering and controller views may diverge if not disciplined to share partials and rendering components.
- **Route registration grows** — Each new Container type adds a controller with 3–5 routes. Mitigated by convention-based routing and a shared `MapEntityRoutes` extension method.
- **URL convention must be enforced** — Ad-hoc routes (e.g., `/Home/Discussions`) create multiple URLs for the same resource. Existing routes must migrate to the new convention over time.

### Neutral

- Both layers use `ComponentHtmlRenderer` under the hood — visual consistency is a byproduct of shared infrastructure, not a separate effort.
- Tag helpers that become too complex (per-entity dashboards, multi-step wizards) should signal the need for a dedicated controller, not grow unbounded widget surface.
- The `/Labels/{labelName}` route is a convenience navigation aid, not a replacement for full search/filter — it redirects to canonical URLs.

---

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| **Tag helpers only, no dedicated controllers** | No SEO metadata, no social preview, no deep linking; every entity is an anonymous content region without a canonical page |
| **Dedicated controllers only, no tag helpers** | No CMS embedding; every content snippet requires a full page; loses the SharePoint web part / composition experience |
| **Razor Pages instead of MVC controllers** | No significant advantage for this pattern; routing conventions work identically; MVC controllers provide richer action filter and model binding infrastructure |
| **Single monolithic `EntityController`** | Requires discriminator-based routing for every action; violates SRP; no per-entity middleware, filters, or authorization policies |
| **Hash-based routing (`#/Forums/1`)** | Breaks server-side routing, SEO, and social sharing; forces JavaScript hydration for every navigation |
| **GraphQL + SPA frontend** | Overengineered for 2–50 person SMB deployments; adds client-side complexity, build tooling, and maintenance burden with no clear benefit over server-rendered HTML |
| **Flat URL scheme (all entities under `/Content/{guid}`)** | No human-readable hierarchy; loses the Container/ContentItem relationship in the URL; GUIDs are not user-friendly for sharing or bookmarking |

---

## Related

- [ADR Domain-Content-Modeling-02](docs/adr/02-Domain-Content-Modeling.md) — Polymorphic content model with dual GUID+int identity that enables the URL scheme and generic tag helper rendering
- [ADR Storage-PgVector-Connector-01](docs/adr/01-Library-Storage-PgVector-Connector.md) — Vector store backend for classification metadata that label navigation will surface
- `docs/OBSERVATIONS.md` #9 — Missing UI primitives that the expanding tag helper library will address
- `AGENTS.md` — Tag helper prefix convention (`iq-`), DI patterns, and project structure
