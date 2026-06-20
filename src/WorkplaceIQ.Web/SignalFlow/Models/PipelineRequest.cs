namespace WorkplaceIQ.Web.SignalFlow.Models;

public record PipelineRequest(
    string ConnectionId,
    PipelineConfig Config,
    CancellationToken Aborted);
