namespace WorkplaceIQ.Metrics;

public sealed record MetricRequest(
    string Name,
    Guid? ContainerId = null,
    string? ContainerType = null,
    string? ContentType = null,
    string? SourceField = null,
    string? Window = null,
    string? Unit = null,
    string? DisplayUnit = null);
