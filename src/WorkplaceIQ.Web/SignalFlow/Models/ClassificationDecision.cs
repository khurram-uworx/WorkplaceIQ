namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed record ClassificationDecision
{
    public ClassificationResult Result { get; init; } = new();
    public string Source { get; init; } = string.Empty;
    public NeighborStats? Stats { get; init; }
    public bool WasAutoLabelled { get; init; }
}
