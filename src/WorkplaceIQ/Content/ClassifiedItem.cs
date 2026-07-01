using System.ComponentModel.DataAnnotations;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Content;

public static class ClassificationSources
{
    public const string Bootstrap = "Bootstrap";
    public const string VectorAuto = "VectorAuto";
    public const string LlmSparseNeighbors = "LlmSparseNeighbors";
    public const string LlmLowConfidence = "LlmLowConfidence";
    public const string LlmEmbeddingFailed = "LlmEmbeddingFailed";
    public const string Failed = "Failed";
}

public sealed class ClassifiedItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContentId { get; set; }
    public ContentItem? RssItem { get; set; }

    public Guid LabelId { get; set; }
    public Label? SignalLabel { get; set; }

    public string? Reasoning { get; set; }
    public bool IsNoise { get; set; }
    public int AttemptCount { get; set; }
    public string? HallucinatedSignal { get; set; }
    public byte[]? Embedding { get; set; }

    [Required]
    [MaxLength(32)]
    public string ClassificationSource { get; set; } = string.Empty;

    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
}
