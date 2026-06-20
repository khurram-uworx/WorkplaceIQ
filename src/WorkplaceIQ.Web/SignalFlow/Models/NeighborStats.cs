namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed record NeighborStats
{
    public int NeighborCount { get; init; }
    public string? TopSignal { get; init; }
    public int TopSignalAgreement { get; init; }
    public double AverageSimilarity { get; init; }
    public double Margin { get; init; }

    public static NeighborStats Empty => new();

    public static NeighborStats From(SignalGroup[] hits)
    {
        if (hits.Length == 0) return Empty;

        var top = hits.OrderByDescending(h => h.Count).First();
        return new NeighborStats
        {
            NeighborCount = hits.Sum(h => h.Count),
            TopSignal = top.Signal,
            TopSignalAgreement = top.Count,
            AverageSimilarity = hits.Average(h => h.AverageSimilarity),
            Margin = hits.Length > 1
                ? top.AverageSimilarity - hits.OrderByDescending(h => h.Count).Skip(1).First().AverageSimilarity
                : top.AverageSimilarity
        };
    }

    public static NeighborStats FromInline(List<(string Signal, double Score)> hits)
    {
        if (hits.Count == 0) return Empty;

        var byCategory = hits
            .GroupBy(h => h.Signal, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Signal: g.Key, Avg: g.Average(h => h.Score), Count: g.Count()))
            .OrderByDescending(g => g.Avg)
            .ThenByDescending(g => g.Count)
            .ToList();

        var top = byCategory[0];
        var second = byCategory.Count > 1 ? byCategory[1] : (Signal: string.Empty, Avg: 0.0, Count: 0);

        return new NeighborStats
        {
            NeighborCount = hits.Count,
            TopSignal = top.Signal,
            TopSignalAgreement = top.Count,
            AverageSimilarity = top.Avg,
            Margin = top.Avg - second.Avg
        };
    }
}
