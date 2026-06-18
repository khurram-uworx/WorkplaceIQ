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
    public async Task ProcessAsync_ResolvesSourceContainerAndRendersMetric()
    {
        var store = new InMemoryWorkplaceIqStore();
        var container = await store.CreateContainerAsync("PowerOutages", ContainerTypes.Feed, "Power Outages");
        var metricService = new RecordingMetricService(new MetricResult(12, "count", null, null));
        var tagHelper = new MetricTagHelper(metricService, store, HtmlEncoder.Default)
        {
            Name = "OutageCountLast7Days",
            Source = "PowerOutages"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(metricService.Name, Is.EqualTo("OutageCountLast7Days"));
        Assert.That(metricService.ContainerId, Is.EqualTo(container.Id));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-metric__value\">12</span>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-metric__unit\">count</span>"));
    }

    [Test]
    public async Task ProcessAsync_RendersDisplayValueWhenPresent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var metricService = new RecordingMetricService(new MetricResult(51120, "seconds", "14.2", "hours"));
        var tagHelper = new MetricTagHelper(metricService, store, HtmlEncoder.Default)
        {
            Name = "TotalOutageTimeLast7Days",
            ContainerId = Guid.NewGuid()
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

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
        public string? Name { get; private set; }

        public Guid? ContainerId { get; private set; }

        public Task<MetricResult> ComputeAsync(
            string name,
            Guid? containerId = null,
            CancellationToken cancellationToken = default)
        {
            Name = name;
            ContainerId = containerId;
            return Task.FromResult(result);
        }
    }
}
