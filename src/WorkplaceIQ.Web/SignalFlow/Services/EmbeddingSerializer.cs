using System.Runtime.InteropServices;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public static class EmbeddingSerializer
{
    public static byte[] ToBytes(ReadOnlyMemory<float> embedding)
    {
        if (embedding.IsEmpty) return [];
        return MemoryMarshal.AsBytes(embedding.Span).ToArray();
    }

    public static ReadOnlyMemory<float> FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0) return ReadOnlyMemory<float>.Empty;
        if (bytes.Length % sizeof(float) != 0)
            throw new ArgumentException("Embedding byte length must be a multiple of 4.", nameof(bytes));
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
