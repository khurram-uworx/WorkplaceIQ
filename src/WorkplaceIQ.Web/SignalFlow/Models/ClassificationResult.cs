namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed record ClassificationResult
{
    public string Signal { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public bool IsNoise { get; init; }
    public string? HallucinatedSignal { get; init; }
}
