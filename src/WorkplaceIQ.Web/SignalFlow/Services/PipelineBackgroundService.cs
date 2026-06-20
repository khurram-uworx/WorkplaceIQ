using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using WorkplaceIQ.Web.Hubs;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public sealed record PipelineState(
    bool IsRunning,
    PipelineEvent? LastEvent);

public sealed class PipelineBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHubContext<PipelineHub> hubContext,
    ILogger<PipelineBackgroundService> logger)
    : BackgroundService
{
    readonly Channel<PipelineRequest> channel = Channel.CreateBounded<PipelineRequest>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    PipelineState currentState = new(false, null);

    public bool TryEnqueue(PipelineRequest request)
        => channel.Writer.TryWrite(request);

    public PipelineState GetState() => currentState;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await RunPipelineAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in pipeline execution loop");
            }
        }
    }

    async Task RunPipelineAsync(PipelineRequest request, CancellationToken ct)
    {
        currentState = new PipelineState(true, null);

        // Don't link request.Aborted — pipeline keeps running if client disconnects
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var combinedCt = cts.Token;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var configLoader = sp.GetRequiredService<ConfigLoader>();
            var orchestrator = sp.GetRequiredService<PipelineOrchestrator>();

            var configDir = Path.Combine(
                sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath,
                request.Config.ConfigDir);
            var config = await configLoader.LoadAsync(configDir, combinedCt);

            var progress = new PipelineProgressReporter(hubContext, evt =>
                currentState = new PipelineState(true, evt));
            await orchestrator.RunAsync(config, progress, combinedCt);

            currentState = new PipelineState(false, null);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Pipeline cancelled");
            currentState = new PipelineState(false, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline failed");
            currentState = new PipelineState(false, null);
            try
            {
                await hubContext.Clients.All
                    .SendAsync("pipelineFailed",
                        new PipelineFailed("Pipeline", ex.Message), CancellationToken.None);
            }
            catch
            {
            }
        }
    }

    sealed class PipelineProgressReporter(
        IHubContext<PipelineHub> hubContext,
        Action<PipelineEvent> onReport) : IProgress<PipelineEvent>
    {
        public void Report(PipelineEvent value)
        {
            onReport(value);

            try
            {
                _ = hubContext.Clients.All
                    .SendAsync("pipelineEvent", value, CancellationToken.None);
            }
            catch
            {
            }
        }
    }
}
