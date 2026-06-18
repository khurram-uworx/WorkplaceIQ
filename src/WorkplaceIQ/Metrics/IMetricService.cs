namespace WorkplaceIQ.Metrics;

public interface IMetricService
{
    Task<MetricResult> ComputeAsync(
        string name,
        Guid? containerId = null,
        CancellationToken cancellationToken = default);
}
