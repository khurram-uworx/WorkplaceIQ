using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Forums;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.Web;

internal static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<WorkplaceIqDbContext>();
        var feedService = services.GetRequiredService<IFeedComponentService>();
        var forumService = services.GetRequiredService<IForumComponentService>();
        var entityService = services.GetRequiredService<IEntityComponentService>();
        var containerService = services.GetRequiredService<IContainerService>();
        var contentItemService = services.GetRequiredService<IContentItemService>();
        var store = services.GetRequiredService<IWorkplaceIqStore>();

        var feed = await feedService.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            true));
        var forum = await forumService.ResolveForumAsync(new ForumComponentRequest(
            "MaintenanceForum",
            "Engineering Forum",
            true));
        var machines = await entityService.ResolveEntitiesAsync(new EntityComponentRequest(
            "Machines",
            "Engineering Assets",
            "Machine",
            true));

        // --- Seed Labels with Colors ---
        var labelSets = new[]
        {
            ("Announcement", "#2563eb"),
            ("Engineering", "#059669"),
            ("Platform", "#7c3aed"),
            ("Infrastructure", "#d97706"),
            ("Maintenance", "#dc2626"),
            ("Deployment", "#0891b2"),
            ("Security", "#ca8a04"),
            ("Performance", "#ea580c"),
            ("Database", "#4f46e5"),
            ("Backend", "#16a34a"),
            ("Frontend", "#9333ea"),
            ("Critical", "#dc2626"),
            ("Incident", "#0f766e"),
            ("Architecture", "#be123c"),
        };

        foreach (var (name, color) in labelSets)
        {
            var normalized = name.ToUpperInvariant();
            var existing = await dbContext.Labels.FirstOrDefaultAsync(l => l.NormalizedName == normalized);
            if (existing is null)
            {
                dbContext.Labels.Add(new Label
                {
                    Name = name,
                    NormalizedName = normalized,
                    Slug = name.ToLowerInvariant().Replace(' ', '-'),
                    Color = color,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await dbContext.SaveChangesAsync();

        // --- Seed Company News Posts ---
        if (feed.Container is not null)
        {
            var existingFeedCount = await dbContext.ContentItems
                .CountAsync(item => item.ContainerId == feed.Container.Id);

            if (existingFeedCount == 0)
            {
                await feedService.CreatePostAsync(
                    "CompanyNews", "Welcome to Workplace Inc.",
                    "Our internal portal is live. Use this space for company announcements, team updates, and cross-team collaboration.",
                    "Announcement, Engineering");
                await feedService.CreatePostAsync(
                    "CompanyNews", "Platform v2.3 deployment completed",
                    "The Platform team has shipped v2.3 with improved caching, reduced latency, and a new API gateway. See the deployment runbook for rollback steps.",
                    "Platform, Deployment, Engineering");
                await feedService.CreatePostAsync(
                    "CompanyNews", "Security audit — action items",
                    "The Q2 security audit identified 3 medium-priority findings. Engineering leads should review by Friday.",
                    "Security, Engineering");
                await feedService.CreatePostAsync(
                    "CompanyNews", "Database migration window next weekend",
                    "Planned migration of the primary PostgreSQL cluster. Downtime window: Saturday 2-4 AM UTC. All services should handle connection retries.",
                    "Database, Infrastructure, Engineering");
            }
        }

        // --- Seed Engineering Forum Threads ---
        if (forum.Container is not null)
        {
            var existingForumCount = await dbContext.ContentItems
                .CountAsync(item => item.ContainerId == forum.Container.Id);

            if (existingForumCount == 0)
            {
                await forumService.CreateThreadAsync(
                    "MaintenanceForum", "API rate limiting design review",
                    "We need to finalize the rate limiting strategy for the public API. Options: token bucket, sliding window, or hybrid. Discuss requirements and trade-offs.",
                    "Engineering, Architecture, Backend");
                await forumService.CreateThreadAsync(
                    "MaintenanceForum", "Frontend bundle optimization",
                    "Our main bundle is 2.4 MB gzipped. Let's discuss code splitting, tree shaking, and lazy loading strategies to bring it under 1 MB.",
                    "Engineering, Frontend, Performance");
                await forumService.CreateThreadAsync(
                    "MaintenanceForum", "Incident post-mortem: DB connection pool exhaustion",
                    "Last week's outage was caused by connection pool exhaustion after a deployment spike. Root cause, timeline, and prevention items are documented here.",
                    "Incident, Database, Infrastructure");
            }
        }

        // --- Seed Engineering Assets ---
        if (machines.Container is not null)
        {
            var existingMachines = await store.GetItemsByContainerAsync(machines.Container.Id);
            if (existingMachines.Count == 0)
            {
                var team = await entityService.CreateEntityAsync(new EntityCreateRequest(
                    "Machines",
                    "Machine",
                    "platform-team",
                    "Platform Team",
                    "Owns the API gateway, service mesh, and deployment infrastructure.",
                    MetadataJson: "{\"lead\":\"Alice Chen\",\"members\":8,\"slack\":\"#platform-eng\"}",
                    Labels: "Engineering, Platform"));
                var backend = await entityService.CreateEntityAsync(new EntityCreateRequest(
                    "Machines",
                    "Machine",
                    "backend-svc",
                    "Backend API",
                    "Core REST API serving all client applications. Node.js + PostgreSQL.",
                    MetadataJson: "{\"language\":\"C#\",\"framework\":\"ASP.NET Core\",\"repository\":\"github.com/workplace/backend\"}",
                    Labels: "Engineering, Backend, Critical"));

                await entityService.CreateRelationshipAsync(backend.Id, team.Id, "owned by");
            }
        }

        // --- Seed Incidents Container + Content ---
        var incidentsContainer = await containerService.GetByNameAsync<Container>("PowerOutages");
        if (incidentsContainer is null)
        {
            incidentsContainer = await store.CreateContainerAsync(new FeedContent
            {
                Name = "PowerOutages",
                Title = "Recent Incidents"
            });
        }

        var incidents = new[]
        {
            (title: "Production DB connection pool exhaustion", severity: "Critical", duration: 5400, machine: "Primary-PostgreSQL", location: "us-east-1", shift: "Night", ago: 1),
            (title: "API gateway latency spike", severity: "High", duration: 7200, machine: "API-Gateway", location: "us-east-1", shift: "Day", ago: 2),
            (title: "Cache cluster failover", severity: "Medium", duration: 3600, machine: "Redis-Cluster", location: "us-west-2", shift: "Night", ago: 3),
            (title: "Deployment pipeline stuck", severity: "High", duration: 4800, machine: "CI-Runner", location: "us-east-1", shift: "Night", ago: 4),
            (title: "CDN certificate expired", severity: "High", duration: 9000, machine: "CDN-Edge", location: "global", shift: "Day", ago: 5),
            (title: "Message queue backpressure", severity: "Medium", duration: 1800, machine: "RabbitMQ-Cluster", location: "us-east-1", shift: "Night", ago: 6),
            (title: "Primary region network outage", severity: "Critical", duration: 10800, machine: "AWS-DirectConnect", location: "us-east-1", shift: "Night", ago: 7),
            (title: "Scheduled maintenance window", severity: "Low", duration: 2400, machine: "Kubernetes-Cluster", location: "us-east-1", shift: "Day", ago: 10),
            (title: "Search index inconsistency", severity: "Medium", duration: 1200, machine: "Elasticsearch-Cluster", location: "us-west-2", shift: "Night", ago: 12),
            (title: "Auto-scaling failure during traffic spike", severity: "Critical", duration: 3600, machine: "AutoScaler", location: "us-east-1", shift: "Night", ago: 14),
            (title: "Database replica lag", severity: "High", duration: 5400, machine: "PostgreSQL-Replica", location: "us-west-2", shift: "Day", ago: 20),
            (title: "Routine backup verification", severity: "Low", duration: 900, machine: "Backup-Job", location: "us-east-1", shift: "Day", ago: 25),
        };

        var existingIncidentContent = await store.GetItemsByContainerAsync(incidentsContainer.Id);
        foreach (var (title, severity, duration, machine, location, shift, ago) in incidents)
        {
            var createdAt = DateTime.UtcNow.Date.AddDays(-ago);
            var metadata = $"{{\"durationSeconds\":{duration},\"machineId\":\"{machine}\",\"location\":\"{location}\",\"shift\":\"{shift}\",\"severity\":\"{severity}\"}}";
            var name = title.ToLowerInvariant().Replace(' ', '-');
            var existing = existingIncidentContent.FirstOrDefault(item => item.Name == name);

            if (existing is null)
            {
                existing = await contentItemService.CreateAsync(new ContentItem
                {
                    ContainerId = incidentsContainer.Id,
                    Discriminator = "Outage",
                    Name = name,
                    Title = title,
                    Body = $"{severity}-severity incident affecting {machine} in {location} during {shift.ToLowerInvariant()} shift. Duration: {duration / 60} minutes.",
                    AuthorUserId = "system",
                    ContentData = metadata,
                    CreatedAt = createdAt,
                    ModifiedAt = createdAt,
                    PublishedAt = createdAt.AddMinutes(5)
                });
            }
            else
            {
                existing.CreatedAt = createdAt;
                existing.ModifiedAt = createdAt;
                existing.PublishedAt = createdAt.AddMinutes(5);
                existing.ContentData = metadata;
                await store.UpdateItemAsync(existing);
            }
        }

        // --- Seed Metric Definitions ---
        var existingMetrics = await dbContext.MetricDefinitions.AnyAsync();
        if (!existingMetrics)
        {
            dbContext.MetricDefinitions.AddRange(
                new MetricDefinition
                {
                    Name = MetricNames.ContainerContentCount,
                    ContainerType = "FeedContainer",
                    InstrumentKind = "Gauge",
                    Aggregation = "Count",
                    Unit = "count",
                    Description = "Content item count by container"
                },
                new MetricDefinition
                {
                    Name = MetricNames.MetadataSum,
                    ContainerType = "FeedContainer",
                    InstrumentKind = "Gauge",
                    Aggregation = "Sum",
                    Unit = "seconds",
                    Description = "Metadata field sum by container"
                }
            );
            await dbContext.SaveChangesAsync();
        }
    }
}
