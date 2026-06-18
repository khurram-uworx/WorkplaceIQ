namespace WorkplaceIQ.Tests.Services;

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorkplaceIQ.Content;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Tests.TestDoubles;

public class MetricServiceTests
{
    private static readonly Guid ContainerId = Guid.NewGuid();

    [Test]
    public async Task ComputeAsync_ReturnsZero_WhenDefinitionNotFound()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("NonExistentMetric", ContainerId);

        Assert.That(result.Value, Is.EqualTo(0));
        Assert.That(result.Unit, Is.EqualTo("count"));
        Assert.That(result.DisplayValue, Is.EqualTo("0"));
        Assert.That(result.DisplayUnit, Is.Null);
    }

    [Test]
    public async Task ComputeAsync_ReturnsZero_WhenNoItemsInContainer()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "TestCount",
            InstrumentKind = "Counter",
            Aggregation = "Count",
            Unit = "count"
        });
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("TestCount", ContainerId);

        Assert.That(result.Value, Is.EqualTo(0));
        Assert.That(result.DisplayValue, Is.Null);
    }

    [Test]
    public async Task ComputeAsync_ReturnsCount_WhenAggregationIsCount()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "ItemCount",
            InstrumentKind = "Counter",
            Aggregation = "Count",
            Unit = "count"
        });
        AddContentItems(store, 5);
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("ItemCount", ContainerId);

        Assert.That(result.Value, Is.EqualTo(5));
    }

    [Test]
    public async Task ComputeAsync_ReturnsSum_WhenAggregationIsSum()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "TotalDuration",
            InstrumentKind = "Histogram",
            Aggregation = "Sum",
            SourceField = "durationSeconds",
            Unit = "seconds"
        });
        AddContentItems(store, 3, durationSeconds: [10, 20, 30]);
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("TotalDuration", ContainerId);

        Assert.That(result.Value, Is.EqualTo(60));
    }

    [Test]
    public async Task ComputeAsync_ReturnsAverage_WhenAggregationIsAvg()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "AvgDuration",
            InstrumentKind = "Histogram",
            Aggregation = "Avg",
            SourceField = "durationSeconds",
            Unit = "seconds"
        });
        AddContentItems(store, 4, durationSeconds: [10, 20, 30, 40]);
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("AvgDuration", ContainerId);

        Assert.That(result.Value, Is.EqualTo(25));
    }

    [Test]
    public async Task ComputeAsync_ReturnsMin_WhenAggregationIsMin()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "MinDuration",
            InstrumentKind = "Histogram",
            Aggregation = "Min",
            SourceField = "durationSeconds",
            Unit = "seconds"
        });
        AddContentItems(store, 3, durationSeconds: [50, 10, 30]);
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("MinDuration", ContainerId);

        Assert.That(result.Value, Is.EqualTo(10));
    }

    [Test]
    public async Task ComputeAsync_ReturnsMax_WhenAggregationIsMax()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "MaxDuration",
            InstrumentKind = "Histogram",
            Aggregation = "Max",
            SourceField = "durationSeconds",
            Unit = "seconds"
        });
        AddContentItems(store, 3, durationSeconds: [5, 15, 25]);
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("MaxDuration", ContainerId);

        Assert.That(result.Value, Is.EqualTo(25));
    }

    [Test]
    public async Task ComputeAsync_ReturnsConvertedDisplayValue_WhenDisplayUnitSet()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "TotalDurationHours",
            InstrumentKind = "Histogram",
            Aggregation = "Sum",
            SourceField = "durationSeconds",
            Unit = "seconds",
            DisplayUnit = "hours"
        });
        AddContentItems(store, 2, durationSeconds: [3600, 7200]);
        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("TotalDurationHours", ContainerId);

        Assert.That(result.Value, Is.EqualTo(10800));
        Assert.That(result.DisplayValue, Is.EqualTo("3.0"));
        Assert.That(result.DisplayUnit, Is.EqualTo("hours"));
    }

    [Test]
    public async Task ComputeAsync_AppliesProviderFilter_BeforeAggregation()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "HighSeverityCount",
            InstrumentKind = "Counter",
            Aggregation = "Count",
            Unit = "count"
        });
        AddContentItems(store, 3, severities: ["low", "high", "high"]);

        var provider = new TestMetricProvider("HighSeverityCount",
            item => item.MetadataJson != null && item.MetadataJson.Contains("\"high\""));

        var service = CreateMetricService(store, [provider]);

        var result = await service.ComputeAsync("HighSeverityCount", ContainerId);

        Assert.That(result.Value, Is.EqualTo(2));
    }

    [Test]
    public async Task ComputeAsync_ReturnsSumOverFilteredItems_WithProviderFilter()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "HighSeverityDuration",
            InstrumentKind = "Histogram",
            Aggregation = "Sum",
            SourceField = "durationSeconds",
            Unit = "seconds"
        });
        AddContentItems(store, 4,
            durationSeconds: [100, 200, 300, 400],
            severities: ["low", "high", "high", "low"]);

        var provider = new TestMetricProvider("HighSeverityDuration",
            item => item.MetadataJson != null && item.MetadataJson.Contains("\"high\""));

        var service = CreateMetricService(store, [provider]);

        var result = await service.ComputeAsync("HighSeverityDuration", ContainerId);

        Assert.That(result.Value, Is.EqualTo(500));
    }

    [Test]
    public async Task ComputeAsync_SkipsItemsWithMissingMetadataField()
    {
        var store = new InMemoryWorkplaceIqStore();
        store.MetricDefinitions.Add(new MetricDefinition
        {
            Name = "SumDuration",
            InstrumentKind = "Histogram",
            Aggregation = "Sum",
            SourceField = "durationSeconds",
            Unit = "seconds"
        });

        store.ContentItems.Add(new ContentItem
        {
            ContainerId = ContainerId,
            ContentType = "Outage",
            Name = "Item-1",
            Title = "Item 1",
            Status = "published",
            MetadataJson = """{"durationSeconds": 100, "severity": "high"}"""
        });
        store.ContentItems.Add(new ContentItem
        {
            ContainerId = ContainerId,
            ContentType = "Outage",
            Name = "Item-2",
            Title = "Item 2",
            Status = "published",
            MetadataJson = """{"severity": "low"}"""
        });
        store.ContentItems.Add(new ContentItem
        {
            ContainerId = ContainerId,
            ContentType = "Outage",
            Name = "Item-3",
            Title = "Item 3",
            Status = "published",
            MetadataJson = """{"durationSeconds": 50, "severity": "medium"}"""
        });

        var service = CreateMetricService(store);

        var result = await service.ComputeAsync("SumDuration", ContainerId);

        Assert.That(result.Value, Is.EqualTo(150));
    }

    private static void AddContentItems(
        InMemoryWorkplaceIqStore store,
        int count,
        double[]? durationSeconds = null,
        string[]? severities = null)
    {
        for (var i = 0; i < count; i++)
        {
            var duration = durationSeconds is not null && i < durationSeconds.Length
                ? durationSeconds[i]
                : (double?)(10 * (i + 1));

            var severity = severities is not null && i < severities.Length
                ? severities[i]
                : "medium";

            store.ContentItems.Add(new ContentItem
            {
                ContainerId = ContainerId,
                ContentType = "Outage",
                Name = $"Item-{i + 1}",
                Title = $"Item {i + 1}",
                Status = "published",
                MetadataJson = $$"""{"durationSeconds": {{duration}}, "severity": "{{severity}}"}"""
            });
        }
    }

    private static MetricService CreateMetricService(
        InMemoryWorkplaceIqStore store,
        IEnumerable<IMetricProvider>? providers = null)
    {
        return new MetricService(
            store,
            providers ?? [],
            NullLogger<MetricService>.Instance);
    }

    private sealed class TestMetricProvider : IMetricProvider
    {
        public string Name { get; }
        public Expression<Func<ContentItem, bool>>? Filter { get; }

        public TestMetricProvider(string name, Expression<Func<ContentItem, bool>> filter)
        {
            Name = name;
            Filter = filter;
        }
    }
}
