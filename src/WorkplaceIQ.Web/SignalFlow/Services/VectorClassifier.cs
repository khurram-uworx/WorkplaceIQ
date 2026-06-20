using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using WorkplaceIQ.Content;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public sealed class VectorClassifier
{
    public delegate Task<ClassificationResult> LlmFallbackDelegate(RssItem item, CancellationToken ct);

    public const int DefaultBootstrapThreshold = 20;
    public const int DefaultTopK = 10;
    public const int DefaultMinNeighbors = 5;
    public const int DefaultMinNeighborAgreement = 5;
    public const double DefaultMinAvgSimilarity = 0.86;
    public const double DefaultMinMargin = 0.10;

    readonly VectorStoreCollection<string, SignalFlowVectorEntry> collection;
    readonly LlmFallbackDelegate llmFallback;
    readonly HashSet<string> validSignals;
    readonly CategoryCentroidTracker? centroids;

    readonly int bootstrapThreshold;
    readonly int topK;
    readonly int minNeighbors;
    readonly int minNeighborAgreement;
    readonly double minAvgSimilarity;
    readonly double minMargin;

    public VectorClassifier(
        VectorStoreCollection<string, SignalFlowVectorEntry> collection,
        LlmFallbackDelegate llmFallback,
        IEnumerable<string> validSignals,
        CategoryCentroidTracker? centroids = null,
        int bootstrapThreshold = DefaultBootstrapThreshold,
        int topK = DefaultTopK,
        int minNeighbors = DefaultMinNeighbors,
        int minNeighborAgreement = DefaultMinNeighborAgreement,
        double minAvgSimilarity = DefaultMinAvgSimilarity,
        double minMargin = DefaultMinMargin)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(llmFallback);
        ArgumentNullException.ThrowIfNull(validSignals);

        this.collection = collection;

        this.llmFallback = llmFallback;
        this.validSignals = new HashSet<string>(validSignals, StringComparer.OrdinalIgnoreCase);
        if (this.validSignals.Count == 0)
            throw new ArgumentException("At least one valid signal is required.", nameof(validSignals));

        this.centroids = centroids;
        this.bootstrapThreshold = bootstrapThreshold >= 0 ? bootstrapThreshold : throw new ArgumentOutOfRangeException(nameof(bootstrapThreshold));
        this.topK = topK > 0 ? topK : throw new ArgumentOutOfRangeException(nameof(topK));
        this.minNeighbors = minNeighbors > 0 ? minNeighbors : throw new ArgumentOutOfRangeException(nameof(minNeighbors));
        if (minNeighbors > topK) throw new ArgumentException("minNeighbors must be <= topK.", nameof(minNeighbors));
        this.minNeighborAgreement = minNeighborAgreement > 0 ? minNeighborAgreement : throw new ArgumentOutOfRangeException(nameof(minNeighborAgreement));
        if (minNeighborAgreement > minNeighbors) throw new ArgumentException("minNeighborAgreement must be <= minNeighbors.", nameof(minNeighborAgreement));
        this.minAvgSimilarity = minAvgSimilarity;
        this.minMargin = minMargin;
    }

    public static VectorClassifier Create(
        VectorStoreCollection<string, SignalFlowVectorEntry> collection,
        IChatClient chatClient,
        string systemPrompt,
        IEnumerable<string> validSignals,
        CategoryCentroidTracker? centroids = null,
        int bootstrapThreshold = DefaultBootstrapThreshold)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentException.ThrowIfNullOrEmpty(systemPrompt);

        return new VectorClassifier(
            collection,
            (item, ct) => RssClassifier.ClassifyAsync(chatClient, item, systemPrompt, ct),
            validSignals,
            centroids,
            bootstrapThreshold);
    }

    public async Task<ClassificationDecision> ClassifyAsync(
        RssItem item,
        ReadOnlyMemory<float> embedding,
        int totalClassifiedCount,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (embedding.IsEmpty) throw new ArgumentException("Embedding must not be empty.", nameof(embedding));

        if (totalClassifiedCount < bootstrapThreshold)
        {
            var bootstrapResult = await llmFallback(item, ct).ConfigureAwait(false);
            return new ClassificationDecision
            {
                Result = bootstrapResult,
                Source = ClassificationSources.Bootstrap,
                Stats = NeighborStats.Empty
            };
        }

        await collection.EnsureCollectionExistsAsync(ct).ConfigureAwait(false);

        var options = new VectorSearchOptions<SignalFlowVectorEntry>
        {
            Filter = e => !e.IsNoise
        };

        var hits = new List<(string Signal, double Score)>(topK);
        await foreach (var result in collection.SearchAsync<ReadOnlyMemory<float>>(embedding, topK, options, ct).ConfigureAwait(false))
        {
            hits.Add((result.Record.Signal, result.Score ?? 0));
            if (hits.Count >= topK) break;
        }

        if (hits.Count < minNeighbors)
        {
            var sparse = await llmFallback(item, ct).ConfigureAwait(false);
            return new ClassificationDecision
            {
                Result = sparse,
                Source = ClassificationSources.LlmSparseNeighbors,
                Stats = NeighborStats.FromInline(hits)
            };
        }

        var top = hits.Take(minNeighbors).ToList();
        var stats = NeighborStats.FromInline(top);

        var passesGates =
            stats.TopSignal is not null && validSignals.Contains(stats.TopSignal) &&
            stats.AverageSimilarity >= minAvgSimilarity &&
            stats.TopSignalAgreement >= minNeighborAgreement &&
            stats.Margin >= minMargin;

        if (!passesGates)
        {
            var lowConfidence = await llmFallback(item, ct).ConfigureAwait(false);
            return new ClassificationDecision
            {
                Result = lowConfidence,
                Source = ClassificationSources.LlmLowConfidence,
                Stats = stats
            };
        }

        var centroidAgreement = stats.TopSignal is not null
            ? TryGetCentroidAgreement(stats.TopSignal, embedding) : null;
        var reasoning =
            $"vector-auto: top={stats.TopSignal} avgSim={stats.AverageSimilarity:F3} " +
            $"agreement={stats.TopSignalAgreement}/{top.Count} margin={stats.Margin:F3}" +
            (centroidAgreement is null ? "" : $" centroid={centroidAgreement.Value.Signal}@{centroidAgreement.Value.Score:F3}");

        var auto = new ClassificationResult
        {
            Signal = stats.TopSignal,
            Reasoning = reasoning,
            IsNoise = false
        };
        return new ClassificationDecision
        {
            Result = auto,
            Source = ClassificationSources.VectorAuto,
            Stats = stats,
            WasAutoLabelled = true
        };
    }

    (string Signal, float Score)? TryGetCentroidAgreement(string topSignal, ReadOnlyMemory<float> embedding)
    {
        if (centroids is null) return null;
        var match = centroids.GetBestCentroidMatch(embedding, minSimilarity: 0.0f);
        if (match is null) return null;
        return string.Equals(match.Value.Signal, topSignal, StringComparison.OrdinalIgnoreCase)
            ? match
            : null;
    }
}
