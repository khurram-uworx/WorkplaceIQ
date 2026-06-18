namespace WorkplaceIQ.Metrics;

public interface IMetricService
{
    Task<MetricResult> ComputeAsync(
        MetricRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetricResult>> ComputeSeriesAsync(
        MetricRequest request,
        CancellationToken cancellationToken = default);
}
