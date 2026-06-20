using Microsoft.Extensions.VectorData;

namespace WorkplaceIQ.Web.SignalFlow.Models;

public static class SignalFlowVectorSchema
{
    public static VectorStoreCollectionDefinition CreateEntryDefinition(int dimension)
    {
        return new()
        {
            Properties =
            [
                new VectorStoreKeyProperty("Id", typeof(string)),
                new VectorStoreDataProperty("Signal", typeof(string)),
                new VectorStoreDataProperty("Title", typeof(string)),
                new VectorStoreDataProperty("Summary", typeof(string)),
                new VectorStoreDataProperty("IsNoise", typeof(bool)),
                new VectorStoreDataProperty("ClassifiedAt", typeof(DateTimeOffset)),
                new VectorStoreVectorProperty("Embedding", typeof(ReadOnlyMemory<float>), dimension)
                {
                    DistanceFunction = DistanceFunction.CosineDistance
                }
            ]
        };
    }
}
