using System.ComponentModel.DataAnnotations;

namespace WorkplaceIQ.Metrics;

public sealed class MetricDefinition
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? ContainerType { get; set; }

    [Required]
    [MaxLength(32)]
    public string InstrumentKind { get; set; } = "Counter";

    [MaxLength(128)]
    public string? SourceField { get; set; }

    [Required]
    [MaxLength(32)]
    public string Aggregation { get; set; } = "Count";

    [Required]
    [MaxLength(32)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? DisplayUnit { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
