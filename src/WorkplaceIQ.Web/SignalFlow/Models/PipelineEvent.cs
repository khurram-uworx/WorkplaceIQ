using System.Text.Json.Serialization;

namespace WorkplaceIQ.Web.SignalFlow.Models;

[JsonDerivedType(typeof(PipelineStarted), "started")]
[JsonDerivedType(typeof(PipelineProgress), "progress")]
[JsonDerivedType(typeof(PipelineItemProcessed), "itemProcessed")]
[JsonDerivedType(typeof(PipelineFailed), "failed")]
[JsonDerivedType(typeof(PipelineCompleted), "completed")]
public abstract record PipelineEvent
{
    public string Type => GetType().Name.Replace("Pipeline", "").ToLowerInvariant();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record PipelineStarted(int TotalFeeds) : PipelineEvent;

public sealed record PipelineProgress(
    string Stage,
    int Current,
    int Total,
    string? Message = null
) : PipelineEvent;

public sealed record PipelineItemProcessed(
    Guid ContentId,
    string Title,
    string Signal,
    bool IsNoise,
    string? Reasoning,
    string? HallucinatedSignal = null
) : PipelineEvent
{
    public string SignalOrNoise => IsNoise ? "noise" : Signal;
}

public sealed record PipelineFailed(string Stage, string Error, Guid? ContentId = null) : PipelineEvent;

public sealed record PipelineCompleted(
    int TotalItems,
    int SignalCount,
    int NoiseCount,
    int FailedCount,
    Dictionary<string, int>? SignalBreakdown = null
) : PipelineEvent;
