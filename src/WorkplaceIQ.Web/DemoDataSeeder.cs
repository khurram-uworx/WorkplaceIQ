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
        var store = services.GetRequiredService<IWorkplaceIqStore>();

        var feed = await feedService.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            true));
        var forum = await forumService.ResolveForumAsync(new ForumComponentRequest(
            "MaintenanceForum",
            "Maintenance Forum",
            true));
        var machines = await entityService.ResolveEntitiesAsync(new EntityComponentRequest(
            "Machines",
            "Machines",
            "Machine",
            true));

        // --- Seed Labels with Colors ---
        var labelSets = new[]
        {
            ("Announcement", "#2563eb"),
            ("Operations", "#059669"),
            ("Platform", "#7c3aed"),
            ("Labels", "#d97706"),
            ("Maintenance", "#dc2626"),
            ("Facilities", "#0891b2"),
            ("Safety", "#ca8a04"),
            ("Power", "#ea580c"),
            ("Generator", "#4f46e5"),
            ("Factory-A", "#16a34a"),
            ("Night-Shift", "#9333ea"),
            ("Severe", "#dc2626"),
            ("Equipment", "#0f766e"),
            ("Critical", "#be123c"),
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
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }
        await dbContext.SaveChangesAsync();

        // --- Seed Feed Posts ---
        if (feed.Container is not null)
        {
            var existingFeedCount = await dbContext.Posts
                .CountAsync(post => post.ContainerId == feed.Container.Id);

            if (existingFeedCount == 0)
            {
                await feedService.CreatePostAsync(
                    "CompanyNews", "Welcome to WorkplaceIQ",
                    "This feed is backed by a database and rendered through a WorkplaceIQ Tag Helper.",
                    "Announcement, Operations");
                await feedService.CreatePostAsync(
                    "CompanyNews", "Declarative components",
                    "The page declares a feed with <iq-feed>, and the platform resolves the persisted container.",
                    "Platform");
                await feedService.CreatePostAsync(
                    "CompanyNews", "Shared content model",
                    "Labels, posts, and containers are reused across feed and forum components.",
                    "Platform, Labels");
            }
        }

        // --- Seed Forum Threads ---
        if (forum.Container is not null)
        {
            var existingForumCount = await dbContext.Posts
                .CountAsync(post => post.ContainerId == forum.Container.Id);

            if (existingForumCount == 0)
            {
                await forumService.CreateThreadAsync(
                    "MaintenanceForum", "Lobby HVAC inspection",
                    "Facilities will inspect the lobby HVAC unit before the next tenant event.",
                    "Maintenance, Facilities");
                await forumService.CreateThreadAsync(
                    "MaintenanceForum", "Parking garage lighting",
                    "Report any remaining dark zones after the level two fixture replacement.",
                    "Safety, Maintenance");
            }
        }

        // --- Seed Machine Entities ---
        if (machines.Container is not null)
        {
            var existingMachines = await store.GetChildrenAsync(machines.Container.Id);
            if (existingMachines.Count == 0)
            {
                var line = await entityService.CreateEntityAsync(new EntityCreateRequest(
                    "Machines",
                    "Machine",
                    "line-1",
                    "Line 1",
                    "Primary production line in Factory A.",
                    MetadataJson: "{\"location\":\"Factory A\",\"owner\":\"Operations\"}",
                    Labels: "Equipment"));
                var press = await entityService.CreateEntityAsync(new EntityCreateRequest(
                    "Machines",
                    "Machine",
                    "press-12",
                    "Press 12",
                    "Hydraulic press monitored for outage correlation.",
                    MetadataJson: "{\"location\":\"Factory A\",\"assetTag\":\"P-12\"}",
                    Labels: "Equipment, Critical"));

                await entityService.CreateRelationshipAsync(press.Id, line.Id, "part of");
            }
        }

        // --- Seed Power Outages Container + Content ---
        var outagesContainer = await store.GetContentByNameAsync("PowerOutages");
        if (outagesContainer is null)
        {
            outagesContainer = await store.CreateContentAsync(new Content.Content
            {
                Name = "PowerOutages",
                ContentType = ContentTypes.FeedContainer,
                Title = "Power Outages in Factory"
            });
        }

        var outages = new[]
        {
            (title: "Generator 3 voltage spike", severity: "High", duration: 5400, machine: "Generator-3", location: "Factory A", shift: "Night", ago: 1),
            (title: "Main breaker trip", severity: "Critical", duration: 7200, machine: "Main-Breaker-A", location: "Factory A", shift: "Day", ago: 2),
            (title: "Cooling system failure", severity: "Medium", duration: 3600, machine: "Cooling-Unit-2", location: "Factory B", shift: "Night", ago: 3),
            (title: "Generator 3 overload", severity: "High", duration: 4800, machine: "Generator-3", location: "Factory A", shift: "Night", ago: 4),
            (title: "Transformer outage", severity: "High", duration: 9000, machine: "Transformer-1", location: "Factory A", shift: "Day", ago: 5),
            (title: "UPS battery failure", severity: "Medium", duration: 1800, machine: "UPS-3", location: "Factory B", shift: "Night", ago: 6),
            (title: "Generator 3 shutdown", severity: "Critical", duration: 10800, machine: "Generator-3", location: "Factory A", shift: "Night", ago: 7),
            (title: "Power line maintenance", severity: "Low", duration: 2400, machine: "Line-Main", location: "Factory A", shift: "Day", ago: 10),
            (title: "Voltage fluctuation", severity: "Medium", duration: 1200, machine: "Generator-3", location: "Factory A", shift: "Night", ago: 12),
            (title: "Emergency stop triggered", severity: "Critical", duration: 3600, machine: "Generator-3", location: "Factory A", shift: "Night", ago: 14),
            (title: "Coolant leak", severity: "High", duration: 5400, machine: "Cooling-Unit-1", location: "Factory B", shift: "Day", ago: 20),
            (title: "Routine breaker test", severity: "Low", duration: 900, machine: "Breaker-2", location: "Factory A", shift: "Day", ago: 25),
        };

        var existingOutageContent = await store.GetChildrenAsync(outagesContainer.Id);
        foreach (var (title, severity, duration, machine, location, shift, ago) in outages)
        {
            var createdAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-ago), TimeSpan.Zero);
            var metadata = $"{{\"durationSeconds\":{duration},\"machineId\":\"{machine}\",\"location\":\"{location}\",\"shift\":\"{shift}\",\"severity\":\"{severity}\"}}";
            var name = title.ToLowerInvariant().Replace(' ', '-');
            var existing = existingOutageContent.FirstOrDefault(item => item.Name == name);

            if (existing is null)
            {
                var contentService = services.GetRequiredService<IContentService>();
                existing = await contentService.CreateAsync(
                    outagesContainer.Id,
                    "Outage",
                    name,
                    title,
                    $"Generator {machine} experienced a {severity.ToLowerInvariant()}-severity outage during {shift.ToLowerInvariant()} shift in {location}. Duration: {duration / 60} minutes.",
                    authorUserId: "system",
                    metadataJson: metadata);
            }

            existing.CreatedAt = createdAt;
            existing.UpdatedAt = createdAt;
            existing.PublishedAt = createdAt.AddMinutes(5);
            existing.MetadataJson = metadata;
            await store.UpdateContentAsync(existing);
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
