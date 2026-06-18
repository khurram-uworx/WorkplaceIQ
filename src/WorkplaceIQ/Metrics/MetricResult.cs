namespace WorkplaceIQ.Metrics;

public sealed record MetricResult(
    double Value,
    string Unit,
    string? DisplayValue,
    string? DisplayUnit);
