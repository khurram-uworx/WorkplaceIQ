namespace WorkplaceIQ.Metrics;

public sealed record MetricResult(
    string Name,
    double Value,
    string Unit,
    string? DisplayValue,
    string? DisplayUnit,
    IReadOnlyDictionary<string, object?> Tags);
