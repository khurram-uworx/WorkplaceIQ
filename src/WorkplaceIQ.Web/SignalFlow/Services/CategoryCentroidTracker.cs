using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public sealed class CategoryCentroidTracker
{
    sealed class Centroid
    {
        public readonly float[] Mean;
        public int Count;

        public Centroid(int dimensions) => Mean = new float[dimensions];
    }

    readonly ConcurrentDictionary<string, Centroid> centroids;
    int expectedDimensions;

    public CategoryCentroidTracker()
    {
        centroids = new ConcurrentDictionary<string, Centroid>(StringComparer.OrdinalIgnoreCase);
    }

    public int CategoryCount => centroids.Count;
    public ICollection<string> Categories => centroids.Keys;

    public int GetCount(string signal)
    {
        ArgumentException.ThrowIfNullOrEmpty(signal);
        return centroids.TryGetValue(signal, out var entry) ? Volatile.Read(ref entry.Count) : 0;
    }

    public void AddOrUpdate(string signal, ReadOnlyMemory<float> embedding)
    {
        ArgumentException.ThrowIfNullOrEmpty(signal);
        if (embedding.IsEmpty) throw new ArgumentException("Embedding must not be empty.", nameof(embedding));

        var dims = embedding.Length;
        var prior = Interlocked.CompareExchange(ref expectedDimensions, dims, 0);
        if (prior != 0 && prior != dims)
        {
            throw new ArgumentException(
                $"Embedding dimension {dims} does not match tracker dimension {prior}.",
                nameof(embedding));
        }

        var normalized = Normalize(embedding.Span);

        var entry = centroids.GetOrAdd(signal, _ => new Centroid(dims));
        lock (entry)
        {
            var n = entry.Count;
            var mean = entry.Mean;
            for (var i = 0; i < dims; i++)
            {
                mean[i] = (mean[i] * n + normalized[i]) / (n + 1);
            }
            entry.Count = n + 1;
        }
    }

    public float GetCentroidSimilarity(string signal, ReadOnlyMemory<float> embedding)
    {
        ArgumentException.ThrowIfNullOrEmpty(signal);
        if (!centroids.TryGetValue(signal, out var entry)) return 0f;

        float[] snapshot;
        lock (entry)
        {
            if (entry.Count == 0) return 0f;
            snapshot = (float[])entry.Mean.Clone();
        }
        return CosineSimilarity(snapshot, embedding.Span);
    }

    public (string Signal, float Score)? GetBestCentroidMatch(
        ReadOnlyMemory<float> embedding, float minSimilarity = 0.7f)
    {
        if (embedding.IsEmpty) return null;

        string? bestSignal = null;
        var bestScore = float.NegativeInfinity;

        foreach (var (signal, entry) in centroids)
        {
            float[] snapshot;
            lock (entry)
            {
                if (entry.Count == 0) continue;
                snapshot = (float[])entry.Mean.Clone();
            }
            var score = CosineSimilarity(snapshot, embedding.Span);
            if (score > bestScore)
            {
                bestScore = score;
                bestSignal = signal;
            }
        }

        if (bestSignal is null || bestScore < minSimilarity) return null;
        return (bestSignal, bestScore);
    }

    static float[] Normalize(ReadOnlySpan<float> source)
    {
        var copy = new float[source.Length];
        var sumSq = 0.0;
        for (var i = 0; i < source.Length; i++) sumSq += source[i] * source[i];
        if (sumSq == 0.0) return copy;
        var norm = (float)Math.Sqrt(sumSq);
        for (var i = 0; i < source.Length; i++) copy[i] = source[i] / norm;
        return copy;
    }

    static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length) return 0f;
        var similarity = TensorPrimitives.CosineSimilarity(a, b);
        return float.IsNaN(similarity) ? 0f : similarity;
    }
}
