using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    /// <summary>
    /// Classification is a label assignment to a Content item with provenance metadata.
    /// Invariant: one ClassifiedItem per ContentId — enforced by the store's Upsert behavior.
    /// When refactoring under ADR 02 (unified polymorphic content model), preserve this invariant:
    /// a Content can have exactly one classification label at a time.
    /// </summary>

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// References Content.Id. After ADR 02 refactoring, this may reference a ContentItem or Container
    /// depending on where classification is applied. The uniqueness constraint on ContentId must be preserved.
    /// </summary>
    public Guid ContentId { get; set; }

    [ForeignKey(nameof(ContentId))]
    public Content? RssItem { get; set; }

    /// <summary>
    /// The classification label (signal) assigned to the content. Reclassified by updating this FK in-place.
    /// </summary>
    public Guid LabelId { get; set; }

    [ForeignKey(nameof(LabelId))]
    public Label? SignalLabel { get; set; }

    public string? Reasoning { get; set; }

    /// <summary>
    /// When true, this classification is noise (not a real signal). Noise items are excluded
    /// from dashboard queries but preserved for audit/retry.
    /// </summary>
    public bool IsNoise { get; set; }

    /// <summary>
    /// Number of classification attempts. Used to detect bounced/failed items (>= 5 attempts).
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// When the LLM returned a signal name not in the known signals list, the hallucinated value is captured here.
    /// </summary>
    public string? HallucinatedSignal { get; set; }

    /// <summary>
    /// Embedding vector serialized as byte[]. Temporary — vector data migrates exclusively to the vector store
    /// (pgvector) where each entry carries metadata fields for filtered similarity search.
    /// </summary>
    public byte[]? Embedding { get; set; }

    /// <summary>
    /// Provenance of this classification: how it was determined (vector auto, LLM fallback, bootstrap, etc.).
    /// Stored as a free-text string because classification sources are extensible and not a fixed enum set.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string ClassificationSource { get; set; } = string.Empty;

    /// <summary>
    /// When this classification was made. Used for ordering in dashboard queries.
    /// Updated on reclassification to reflect the latest decision time.
    /// Stored as DateTime (UTC) for SQLite compatibility — SQLite cannot natively order by DateTimeOffset.
    /// </summary>
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
}
