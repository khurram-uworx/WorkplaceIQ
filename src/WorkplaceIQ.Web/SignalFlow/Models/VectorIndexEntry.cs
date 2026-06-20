namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed record VectorIndexEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid RssItemId { get; init; }
    public string Signal { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public ReadOnlyMemory<float> Embedding { get; init; }
}
