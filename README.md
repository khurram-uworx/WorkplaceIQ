# WorkplaceIQ

> Build intelligent intranets where every piece of content becomes searchable, measurable, conversational, and insight-ready.

WorkplaceIQ is an AI-native workplace platform for building modern intranets, knowledge hubs, collaboration portals, operational dashboards, and business applications.

Unlike traditional CMS platforms that focus on pages and documents, WorkplaceIQ focuses on **content, metadata, relationships, and intelligence**.

Every feed, forum, document library, business entity, and operational record is built on a common abstraction that enables:

* Collaboration
* Search
* Analytics
* AI Chat
* Insights
* Automation

---

## Local Docker

Run the demo app with MinIO-backed local infrastructure:

```powershell
docker compose up --build
```

The web app is available at `http://localhost:4792`.

MinIO is available at:

* API: `http://localhost:9000`
* Console: `http://localhost:9001`

Default local credentials:

```text
workplaceiq / workplaceiq-secret
```

The Compose file does not create storage buckets yet. Bucket creation should be handled by the application storage provider when the files slice is implemented.

---

## Vision

Traditional CMS platforms help organizations publish information.

WorkplaceIQ helps organizations understand information.

The platform treats all workplace information as structured content that can be:

* Tagged
* Related
* Measured
* Queried
* Summarized
* Analyzed
* Discussed
* Enhanced by AI

---

## Core Principles

### Everything is Content

WorkplaceIQ is built around a small set of primitives:

```text
Container
 └── Content
      └── Post
           ├── Metadata
           ├── Labels
           ├── Comments
           └── Relationships
```

Examples:

| Business Concept | WorkplaceIQ Representation |
| ---------------- | -------------------------- |
| Company News     | Feed                       |
| Discussion Board | Forum                      |
| HR Policies      | File Library               |
| Machine          | Entity                     |
| Customer         | Entity                     |
| Incident         | Content                    |
| Outage           | Content                    |
| Project          | Container                  |

---

### Everything Has Metadata

Metadata is the foundation of workplace intelligence.

Example:

```json
{
  "durationSeconds": 3600,
  "location": "Factory A",
  "machineId": "GEN-03",
  "severity": "High"
}
```

The platform automatically exposes:

* Counts
* Sums
* Averages
* Trends
* Distributions
* Time-series analytics

without requiring custom reporting code.

---

### Everything is AI Ready

Every content item can be:

* Indexed
* Embedded
* Vectorized
* Summarized
* Queried through natural language

Examples:

> Show me unresolved outages from the last 30 days.

> Which machine caused the highest downtime this quarter?

> Summarize employee feedback regarding safety procedures.

---

## Declarative Development Model

WorkplaceIQ uses ASP.NET Razor Components and Tag Helpers to compose applications declaratively.

Example:

```html
<iq-feed id="PowerOutages"
         title="Power Outages"
         allow-comments="true" />

<iq-forum id="Maintenance"
          title="Maintenance Discussions" />

<iq-files id="SafetyDocuments"
          title="Safety Documents" />

<iq-entity type="Machine" />
```

Behind the scenes the platform provisions:

* Storage
* APIs
* Search
* Permissions
* Metadata
* AI Indexes
* Dashboards
* Analytics

---

## Built-In Components

### Feeds

Activity streams and announcements.

```html
<iq-feed />
```

### Forums

Discussion and collaboration.

```html
<iq-forum />
```

### Files

Document management.

```html
<iq-files />
```

### Entities

Business objects.

```html
<iq-entity type="Customer" />
<iq-entity type="Machine" />
<iq-entity type="Project" />
```

### Dashboards

Metric and insight visualization.

```html
<iq-dashboard />
```

### AI Chat

Chat with content.

```html
<iq-ai-chat source="PowerOutages" />
```

---

## Metadata-Driven Analytics

WorkplaceIQ automatically generates metrics from content metadata.

Example:

```json
{
  "durationSeconds": 1800
}
```

Generated metrics:

```text
Count
Sum
Average
Minimum
Maximum
Trend
Moving Average
Anomaly Detection
```

Business interpretation:

```text
Total Outage Time
Average Outage Duration
Monthly Downtime Trend
```

---

## AI Capabilities

### Semantic Search

Find content based on meaning rather than keywords.

### Chat With Content

Ask questions about:

* Feeds
* Forums
* Files
* Entities
* Projects

### Summaries

Generate:

* Daily summaries
* Weekly digests
* Executive reports

### Insights

Automatically identify:

* Trends
* Risks
* Repeated issues
* Emerging topics
* Operational anomalies

---

## Proposed Technology Stack

### Backend

* ASP.NET Core
* ASP.NET MVC
* Razor Pages
* Razor Components
* Entity Framework Core

### Database

* SQL Server
* PostgreSQL

### Search

* Azure AI Search
* Elasticsearch
* OpenSearch

### AI

* Microsoft.Extensions.AI
* Microsoft.Extensions.VectorData
* Semantic Kernel
* OpenAI
* Azure OpenAI

### Frontend

* Razor Components
* HTMX (optional)
* Blazor (optional)

---

## Repository Structure

```text
src/

  WorkplaceIQ.Core/
  WorkplaceIQ.Domain/
  WorkplaceIQ.Application/
  WorkplaceIQ.Infrastructure/

  WorkplaceIQ.Web/

  WorkplaceIQ.AI/
  WorkplaceIQ.Search/
  WorkplaceIQ.Analytics/

  WorkplaceIQ.TagHelpers/

tests/

docs/

samples/
```

---

## Roadmap

### Phase 1

* Core Content Model
* Containers
* Content
* Posts
* Labels
* Metadata
* Comments

### Phase 2

* Feeds
* Forums
* Files
* Entities
* Permissions

### Phase 3

* Vector Search
* AI Chat
* Summaries

### Phase 4

* Insights Engine
* Analytics Engine
* Recommendation Engine

### Phase 5

* Multi-Tenant SaaS
* Marketplace
* Component Ecosystem

---

## Long-Term Goal

Become the platform organizations use to build intelligent workplaces.

Not just a CMS.

Not just an intranet.

A workplace intelligence platform.
