using Microsoft.Extensions.Logging.Abstractions;
using WorkplaceIQ.Content;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.Services;

public class MetricServiceTests
{
    [Test]
    public async Task ComputeAsync_ReturnsContentCountForSourceContainer()
    {
        var store = new InMemoryWorkplaceIqStore();
        var container = await store.CreateContentAsync(new Content.Content
        {
            Name = "FactoryPowerOutages",
            ContentType = ContentTypes.FeedContainer,
            Title = "Factory Power Outages"
        });
        AddContentItem(store, container.Id, "Outage", createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        AddContentItem(store, container.Id, "Policy", createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync(new MetricRequest(
            MetricNames.ContainerContentCount,
            ContainerId: container.Id,
            ContainerType: ContentTypes.FeedContainer,
            ContentType: "Outage",
            Window: "last_7_days"));

        Assert.That(result.Value, Is.EqualTo(1));
        Assert.That(result.Unit, Is.EqualTo("count"));
        Assert.That(result.Tags["container.name"], Is.EqualTo("FactoryPowerOutages"));
        Assert.That(result.Tags["content.type"], Is.EqualTo("Outage"));
        Assert.That(result.Tags["window"], Is.EqualTo("last_7_days"));
    }

    [Test]
    public async Task ComputeAsync_ReturnsMetadataSumWithDisplayUnit()
    {
        var store = new InMemoryWorkplaceIqStore();
        var container = await store.CreateContentAsync(new Content.Content
        {
            Name = "FactoryPowerOutages",
            ContentType = ContentTypes.FeedContainer,
            Title = "Factory Power Outages"
        });
        AddContentItem(store, container.Id, "Outage", durationSeconds: 3600, createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        AddContentItem(store, container.Id, "Outage", durationSeconds: 7200, createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync(new MetricRequest(
            MetricNames.MetadataSum,
            ContainerId: container.Id,
            ContainerType: ContentTypes.FeedContainer,
            ContentType: "Outage",
            SourceField: "durationSeconds",
            Window: "last_7_days",
            Unit: "seconds",
            DisplayUnit: "hours"));

        Assert.That(result.Value, Is.EqualTo(10800));
        Assert.That(result.DisplayValue, Is.EqualTo("3.0"));
        Assert.That(result.DisplayUnit, Is.EqualTo("hours"));
        Assert.That(result.Tags["metadata.field"], Is.EqualTo("durationSeconds"));
    }

    [Test]
    public async Task ComputeSeriesAsync_ExpandsGenericMetricAcrossMatchingContainers()
    {
        var store = new InMemoryWorkplaceIqStore();
        var factory = await store.CreateContentAsync(new Content.Content
        {
            Name = "FactoryPowerOutages",
            ContentType = ContentTypes.FeedContainer,
            Title = "Factory Power Outages"
        });
        var office = await store.CreateContentAsync(new Content.Content
        {
            Name = "OfficePowerOutages",
            ContentType = ContentTypes.FeedContainer,
            Title = "Office Power Outages"
        });
        var forum = await store.CreateContentAsync(new Content.Content
        {
            Name = "MaintenanceForum",
            ContentType = ContentTypes.ForumContainer,
            Title = "Maintenance Forum"
        });
        AddContentItem(store, factory.Id, "Outage", createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        AddContentItem(store, factory.Id, "Outage", createdAt: DateTimeOffset.UtcNow.AddDays(-2));
        AddContentItem(store, office.Id, "Outage", createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        AddContentItem(store, forum.Id, "Outage", createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        var service = CreateMetricService(store);

        var series = await service.ComputeSeriesAsync(new MetricRequest(
            MetricNames.ContainerContentCount,
            ContainerType: ContentTypes.FeedContainer,
            ContentType: "Outage",
            Window: "last_7_days"));

        Assert.That(series, Has.Count.EqualTo(2));
        Assert.That(series.Single(result => (string)result.Tags["container.name"]! == "FactoryPowerOutages").Value, Is.EqualTo(2));
        Assert.That(series.Single(result => (string)result.Tags["container.name"]! == "OfficePowerOutages").Value, Is.EqualTo(1));
    }

    [Test]
    public async Task ComputeAsync_ReturnsZeroWhenProviderIsMissing()
    {
        var service = CreateMetricService(new InMemoryWorkplaceIqStore());

        var result = await service.ComputeAsync(new MetricRequest("workplaceiq.unknown.metric"));

        Assert.That(result.Value, Is.EqualTo(0));
        Assert.That(result.DisplayValue, Is.EqualTo("0"));
    }

    private static void AddContentItem(
        InMemoryWorkplaceIqStore store,
        Guid containerId,
        string contentType,
        double durationSeconds = 0,
        DateTimeOffset? createdAt = null)
    {
        store.Contents.Add(new Content.Content
        {
            ParentId = containerId,
            ContentType = contentType,
            Name = Guid.NewGuid().ToString("N"),
            Title = contentType,
            Status = "published",
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            MetadataJson = $$"""{"durationSeconds": {{durationSeconds}}}"""
        });
    }

    private static MetricService CreateMetricService(InMemoryWorkplaceIqStore store)
    {
        return new MetricService(
            store,
            [
                new ContentCountMetricProvider(),
                new MetadataAggregationMetricProvider(MetricNames.MetadataSum, values => values.Sum()),
                new MetadataAggregationMetricProvider(MetricNames.MetadataAverage, values => values.Average()),
                new MetadataAggregationMetricProvider(MetricNames.MetadataMin, values => values.Min()),
                new MetadataAggregationMetricProvider(MetricNames.MetadataMax, values => values.Max())
            ],
            NullLogger<MetricService>.Instance);
    }
}
