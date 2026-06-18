namespace WorkplaceIQ.Metrics;

internal static class MetricResultFactory
{
    public static MetricResult Zero(MetricRequest request)
    {
        return new MetricResult(
            request.Name,
            0,
            request.Unit ?? "count",
            "0",
            request.DisplayUnit,
            new Dictionary<string, object?>());
    }
}
