namespace WorkplaceIQ.Metrics;

public interface IMetricProvider
{
    string Name { get; }

    Task<IReadOnlyList<MetricResult>> ComputeSeriesAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken = default);
}
