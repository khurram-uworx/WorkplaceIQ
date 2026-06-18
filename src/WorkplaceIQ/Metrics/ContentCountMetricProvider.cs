namespace WorkplaceIQ.Metrics;

public sealed class ContentCountMetricProvider : MetricProviderBase, IMetricProvider
{
    public string Name => MetricNames.ContainerContentCount;

    public async Task<IReadOnlyList<MetricResult>> ComputeSeriesAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken = default)
    {
        var containerItems = await GetContainerItemsAsync(request, store, cancellationToken);

        return containerItems
            .Select(entry => new MetricResult(
                Name,
                entry.Items.Count,
                request.Unit ?? "count",
                null,
                request.DisplayUnit,
                CreateTags(request, entry.Container)))
            .ToList();
    }
}
