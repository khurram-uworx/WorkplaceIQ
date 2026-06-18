namespace WorkplaceIQ.Tests.TagHelpers;

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Tests.TestDoubles;

public class MetricTagHelperTests
{
    [Test]
    public async Task ProcessAsync_BuildsMetricRequestFromAttributes()
    {
        var store = new InMemoryWorkplaceIqStore();
        var container = await store.CreateContainerAsync("PowerOutages", ContainerTypes.Feed, "Power Outages");
        var metricService = new RecordingMetricService(new MetricResult(
            MetricNames.ContainerContentCount,
            12,
            "count",
            null,
            null,
            new Dictionary<string, object?>()));
        var tagHelper = new MetricTagHelper(metricService, store, HtmlEncoder.Default)
        {
            Name = MetricNames.ContainerContentCount,
            Source = "PowerOutages",
            ContentType = "Outage",
            Window = "last_7_days"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(metricService.Request?.Name, Is.EqualTo(MetricNames.ContainerContentCount));
        Assert.That(metricService.Request?.ContainerId, Is.EqualTo(container.Id));
        Assert.That(metricService.Request?.ContainerType, Is.EqualTo(ContainerTypes.Feed));
        Assert.That(metricService.Request?.ContentType, Is.EqualTo("Outage"));
        Assert.That(metricService.Request?.Window, Is.EqualTo("last_7_days"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-metric__value\">12</span>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-metric__unit\">count</span>"));
    }

    [Test]
    public async Task ProcessAsync_RendersDisplayValueWhenPresent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var metricService = new RecordingMetricService(new MetricResult(
            MetricNames.MetadataSum,
            51120,
            "seconds",
            "14.2",
            "hours",
            new Dictionary<string, object?>()));
        var tagHelper = new MetricTagHelper(metricService, store, HtmlEncoder.Default)
        {
            Name = MetricNames.MetadataSum,
            ContainerId = Guid.NewGuid(),
            SourceField = "durationSeconds",
            Unit = "seconds",
            DisplayUnit = "hours"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(metricService.Request?.SourceField, Is.EqualTo("durationSeconds"));
        Assert.That(metricService.Request?.Unit, Is.EqualTo("seconds"));
        Assert.That(metricService.Request?.DisplayUnit, Is.EqualTo("hours"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-metric__value\">14.2</span>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-metric__unit\">hours</span>"));
    }

    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());
    }

    private sealed class RecordingMetricService(MetricResult result) : IMetricService
    {
        public MetricRequest? Request { get; private set; }

        public Task<MetricResult> ComputeAsync(
            MetricRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<MetricResult>> ComputeSeriesAsync(
            MetricRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult<IReadOnlyList<MetricResult>>([result]);
        }
    }
}
