using WorkplaceIQ.Content;

namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed record SignalGroup
{
    public string Signal { get; init; } = string.Empty;
    public int Count { get; init; }
    public double AverageSimilarity { get; init; }
    public List<ClassifiedItem> Items { get; init; } = [];
}
