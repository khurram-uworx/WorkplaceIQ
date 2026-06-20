using Microsoft.Extensions.VectorData;

namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed class SignalFlowVectorEntry
{
    public const string CollectionName = "signalflow-entries";

    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreData]
    public string Signal { get; set; } = string.Empty;

    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData]
    public string Summary { get; set; } = string.Empty;

    [VectorStoreData]
    public bool IsNoise { get; set; }

    [VectorStoreData]
    public DateTimeOffset ClassifiedAt { get; set; }

    // Compile-time default. Overridden at runtime via SignalFlowVectorSchema.CreateEntryDefinition.
    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float>? Embedding { get; set; }
}
