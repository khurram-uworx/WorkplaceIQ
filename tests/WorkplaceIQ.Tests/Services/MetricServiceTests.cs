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
        var container = await store.CreateContainerAsync(new FeedContent
        {
            Name = "FactoryPowerOutages",
            Title = "Factory Power Outages"
        });
        AddContentItem(store, container.Id, "Outage", createdAt: DateTime.UtcNow.AddDays(-1));
        AddContentItem(store, container.Id, "Policy", createdAt: DateTime.UtcNow.AddDays(-1));
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync(new MetricRequest(
            MetricNames.ContainerContentCount,
            ContainerId: container.Id,
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
        var container = await store.CreateContainerAsync(new FeedContent
        {
            Name = "FactoryPowerOutages",
            Title = "Factory Power Outages"
        });
        AddContentItem(store, container.Id, "Outage", durationSeconds: 3600, createdAt: DateTime.UtcNow.AddDays(-1));
        AddContentItem(store, container.Id, "Outage", durationSeconds: 7200, createdAt: DateTime.UtcNow.AddDays(-1));
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync(new MetricRequest(
            MetricNames.MetadataSum,
            ContainerId: container.Id,
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
        var factory = await store.CreateContainerAsync(new FeedContent
        {
            Name = "FactoryPowerOutages",
            Title = "Factory Power Outages"
        });
        var office = await store.CreateContainerAsync(new FeedContent
        {
            Name = "OfficePowerOutages",
            Title = "Office Power Outages"
        });
        var forum = await store.CreateContainerAsync(new DiscussionContent
        {
            Name = "MaintenanceForum",
            Title = "Maintenance Forum"
        });
        AddContentItem(store, factory.Id, "Outage", createdAt: DateTime.UtcNow.AddDays(-1));
        AddContentItem(store, factory.Id, "Outage", createdAt: DateTime.UtcNow.AddDays(-2));
        AddContentItem(store, office.Id, "Outage", createdAt: DateTime.UtcNow.AddDays(-1));
        AddContentItem(store, forum.Id, "Outage", createdAt: DateTime.UtcNow.AddDays(-1));
        var service = CreateMetricService(store);

        var series = await service.ComputeSeriesAsync(new MetricRequest(
            MetricNames.ContainerContentCount,
            ContentType: "Outage",
            Window: "last_7_days"));

        Assert.That(series, Has.Count.EqualTo(3));
        var factoryResult = series.Single(r => (string)r.Tags["container.name"]! == "FactoryPowerOutages");
        var officeResult = series.Single(r => (string)r.Tags["container.name"]! == "OfficePowerOutages");
        Assert.That(factoryResult.Value, Is.EqualTo(2));
        Assert.That(officeResult.Value, Is.EqualTo(1));
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
        DateTime? createdAt = null)
    {
        store.Items.Add(new ContentItem
        {
            ContainerId = containerId,
            Discriminator = contentType,
            Name = Guid.NewGuid().ToString("N"),
            Title = contentType,
            Status = "published",
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ContentData = $$"""{"durationSeconds": {{durationSeconds}}}"""
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