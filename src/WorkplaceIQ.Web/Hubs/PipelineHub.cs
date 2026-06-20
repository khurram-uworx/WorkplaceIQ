using Microsoft.AspNetCore.SignalR;
using WorkplaceIQ.Web.SignalFlow.Models;
using WorkplaceIQ.Web.SignalFlow.Services;

namespace WorkplaceIQ.Web.Hubs;

public sealed class PipelineHub(
    IFeedbackService feedbackService,
    PipelineBackgroundService pipelineService) : Hub
{
    public Task GetPipelineState()
    {
        return Clients.Caller.SendAsync("pipelineState",
            pipelineService.GetState(), Context.ConnectionAborted);
    }

    public async Task RunPipeline()
    {
        var config = new PipelineConfig();
        var request = new PipelineRequest(
            Context.ConnectionId,
            config,
            Context.ConnectionAborted);

        if (!pipelineService.TryEnqueue(request))
        {
            await Clients.Caller.SendAsync("pipelineEvent",
                new PipelineFailed("Pipeline", "A pipeline is already running."), Context.ConnectionAborted);
        }
    }

    public async Task GetDashboardData()
    {
        var signalCounts = await feedbackService.GetSignalCountsAsync(Context.ConnectionAborted);
        var noiseCount = await feedbackService.GetNoiseCountAsync(Context.ConnectionAborted);
        var bouncedCount = await feedbackService.GetFailedCountAsync(Context.ConnectionAborted);
        var recent = await feedbackService.GetRecentItemsAsync(10, Context.ConnectionAborted);

        await Clients.Caller.SendAsync("dashboardData", new
        {
            SignalNames = signalCounts.Keys.ToList(),
            SignalCounts = signalCounts,
            NoiseCount = noiseCount,
            BouncedCount = bouncedCount,
            RecentItems = recent.Select(i => new
            {
                i.Id,
                i.RssItem?.Title,
                Signal = i.SignalLabel?.Name,
                i.IsNoise,
                i.Reasoning,
                ClassifiedAt = i.ClassifiedAt.ToString("o")
            }).ToList()
        }, Context.ConnectionAborted);
    }

    public async Task ReclassifyItem(string itemId, string newSignal, bool isNoise)
    {
        if (!Guid.TryParse(itemId, out var id)) return;
        await feedbackService.ReclassifyAsync(id, newSignal, isNoise, Context.ConnectionAborted);
    }

    public async Task MarkNotNoise(string itemId)
    {
        if (!Guid.TryParse(itemId, out var id)) return;
        await feedbackService.MarkNotNoiseAsync(id, Context.ConnectionAborted);
    }

    public async Task<bool> RetryItem(string itemId)
    {
        if (!Guid.TryParse(itemId, out var id)) return false;
        var (success, _) = await feedbackService.RetryFailedAsync(id, Context.ConnectionAborted);
        return success;
    }
}
