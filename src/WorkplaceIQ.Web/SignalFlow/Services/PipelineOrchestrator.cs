using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Streamix;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Web.SignalFlow.Models;
using static WorkplaceIQ.Content.ClassificationSources;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public class PipelineOrchestrator(
    IWorkplaceIqStore store,
    IServiceScopeFactory scopeFactory,
    EmbeddingService embeddingService,
    IChatClient chatClient,
    CategoryCentroidTracker centroids,
    VectorStoreCollection<string, SignalFlowVectorEntry> collection,
    ILogger<PipelineOrchestrator> logger)
{

    sealed record EmbeddedWork(
        Content.Content Content,
        RssItem RssItem,
        ReadOnlyMemory<float> Embedding,
        bool EmbeddingSucceeded);

    public async Task<PipelineCompleted> RunAsync(
        PipelineConfig config,
        IProgress<PipelineEvent> progress,
        CancellationToken ct = default)
    {
        var signalsText = string.Join("\n", config.Signals.Select(s => $"- {s}"));
        var validSignals = config.Signals.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var systemPrompt = config.PromptTemplate
            .Replace("{goalText}", config.Goal)
            .Replace("{signalsText}", signalsText);

        progress.Report(new PipelineStarted(config.FeedSources.Count));

        var processed = 0;
        var autoCount = 0;
        var llmCount = 0;
        var failedCount = 0;

        await Flux.ScopedAsync(async scope =>
        {
            var innerCt = scope.CancellationToken;

            progress.Report(new PipelineProgress("Restore", 0, 1, "Restoring vector store from prior classifications"));
            await RestoreVectorStateAsync(innerCt);
            progress.Report(new PipelineProgress("Restore", 1, 1, "Vector store restored"));

            var classifier = new VectorClassifier(
                collection,
                CreateLlmFallback(chatClient, systemPrompt, validSignals),
                validSignals,
                centroids,
                bootstrapThreshold: config.BootstrapThreshold,
                topK: config.TopK,
                minNeighbors: config.MinNeighbors,
                minNeighborAgreement: config.MinNeighborAgreement,
                minAvgSimilarity: config.MinAvgSimilarity,
                minMargin: config.MinMargin);

            // Stage 1 & 2: Parallel RSS fetch + dedup save
            progress.Report(new PipelineProgress("Fetch", 0, config.FeedSources.Count, "Fetching feeds"));

            var savedCount = 0;
            await Flux.From<FeedSource>(config.FeedSources)
                .FlatMap(source => Flux
                    .From(RssFetcher.FetchFeedAsync(source.Url, source.Name, innerCt))
                    .OnErrorResume(ex =>
                    {
                        progress.Report(new PipelineFailed("Fetch", ex.Message));
                        return Flux.Empty<RssItem>();
                    }), maxConcurrency: config.MaxConcurrency)
                .Checkpoint("Fetch")
                .FlatMap(async item =>
                {
                    using var opScope = scopeFactory.CreateScope();
                    var opStore = opScope.ServiceProvider.GetRequiredService<IWorkplaceIqStore>();
                    var existing = await opStore.GetContentByNameAsync(item.ContentHash, innerCt);
                    if (existing is not null) return false;

                    var content = new Content.Content
                    {
                        Name = item.ContentHash,
                        Title = item.Title,
                        Body = item.Summary,
                        ContentType = "RssItem",
                        Status = "active"
                    };
                    await opStore.CreateContentAsync(content, innerCt);
                    Interlocked.Increment(ref savedCount);
                    return true;
                }, maxConcurrency: config.MaxConcurrency)
                .DrainAsync(innerCt);

            progress.Report(new PipelineProgress("Fetch", config.FeedSources.Count, config.FeedSources.Count,
                $"Fetched {savedCount} new items"));

            // Stage 3: Collect unprocessed items
            var unprocessed = new List<Content.Content>();
            await foreach (var c in store.GetUnclassifiedContentsAsync(int.MaxValue, innerCt))
            {
                if (c.ContentType == "RssItem")
                    unprocessed.Add(c);
            }

            if (unprocessed.Count == 0)
            {
                progress.Report(new PipelineCompleted(0, 0, 0, 0));
                return;
            }

            progress.Report(new PipelineProgress("Classify", 0, unprocessed.Count,
                $"Found {unprocessed.Count} items to classify"));

            // Stage 4 & 5: Embed + classify via Streamix Flux
            var signalCounts = await store.GetSignalCountsAsync(innerCt);
            var currentClassified = signalCounts.Sum(kvp => kvp.Value);

            await Flux.From<Content.Content>(unprocessed)
                .Checkpoint("Embed")
                .FlatMap(async content =>
                {
                    var rssItem = new RssItem
                    {
                        Title = content.Title,
                        Summary = content.Body ?? string.Empty,
                        Link = string.Empty,
                        FeedUrl = string.Empty,
                        FeedName = string.Empty,
                        Published = content.CreatedAt,
                        ContentHash = content.Name
                    };

                    try
                    {
                        var embedding = await embeddingService.GenerateAsync(rssItem, innerCt);
                        return new EmbeddedWork(content, rssItem, embedding, true);
                    }
                    catch (Exception ex)
                    {
                        progress.Report(new PipelineFailed("Embed", ex.Message, content.Id));
                        return new EmbeddedWork(content, rssItem, ReadOnlyMemory<float>.Empty, false);
                    }
                }, maxConcurrency: config.MaxConcurrency)
                .Checkpoint("Classify")
                .ForEachAsync(async work =>
                {
                    var attemptCount = 0;
                    var currentCount = currentClassified + processed;

                    async Task<ClassificationDecision> ClassifyAttemptAsync(CancellationToken ct2)
                    {
                        attemptCount++;
                        if (!work.EmbeddingSucceeded)
                        {
                            var llmResult = await ClassifyAndValidateAsync(chatClient, work.RssItem, systemPrompt, validSignals, ct2);
                            return new ClassificationDecision
                            {
                                Result = llmResult,
                                Source = LlmEmbeddingFailed,
                                Stats = NeighborStats.Empty
                            };
                        }

                        return await classifier.ClassifyAsync(work.RssItem, work.Embedding, currentCount, ct2);
                    }

                    ClassificationDecision decision;
                    try
                    {
                        decision = await Flux.FromTask(ClassifyAttemptAsync)
                            .RetryThenReturn(3, ex =>
                            {
                                var hallucinated = ex.Data["HallucinatedSignal"] as string;
                                return new ClassificationDecision
                                {
                                    Result = new ClassificationResult
                                    {
                                        Signal = "General",
                                        Reasoning = $"All 3 attempts failed. Last invalid signal: '{hallucinated}'",
                                        IsNoise = true,
                                        HallucinatedSignal = hallucinated
                                    },
                                    Source = Failed,
                                    Stats = NeighborStats.Empty
                                };
                            })
                            .ToTask(innerCt);

                        if (decision.WasAutoLabelled) Interlocked.Increment(ref autoCount);
                        else Interlocked.Increment(ref llmCount);
                    }
                    catch (Exception)
                    {
                        decision = new ClassificationDecision
                        {
                            Result = new ClassificationResult
                            {
                                Signal = "General",
                                Reasoning = "Classification failed",
                                IsNoise = true
                            },
                            Source = Failed,
                            Stats = NeighborStats.Empty
                        };
                        Interlocked.Increment(ref failedCount);
                    }

                    await PersistResultAsync(work.Content, work.Embedding, decision, attemptCount, innerCt);
                    Interlocked.Increment(ref processed);

                    progress.Report(new PipelineItemProcessed(
                        work.Content.Id, work.RssItem.Title, decision.Result.Signal,
                        decision.Result.IsNoise, decision.Result.Reasoning,
                        decision.Result.HallucinatedSignal));

                    progress.Report(new PipelineProgress("Classify", processed, unprocessed.Count,
                        $"[{(decision.WasAutoLabelled ? "auto" : "llm")}] {work.RssItem.Title} \u2192 {decision.Result.Signal}"));
                }, maxConcurrency: 1, cancellationToken: innerCt);
        }, ct);

        var completed = new PipelineCompleted(
            TotalItems: processed,
            SignalCount: processed - failedCount,
            NoiseCount: 0,
            FailedCount: failedCount);

        progress.Report(completed);
        logger.LogInformation(
            "Pipeline completed: {Processed} items, {Auto} auto, {Llm} llm, {Failed} failed",
            processed, autoCount, llmCount, failedCount);

        return completed;
    }

    static async Task<ClassificationResult> ClassifyAndValidateAsync(
        IChatClient client, RssItem item, string systemPrompt,
        HashSet<string> validSignals, CancellationToken ct)
    {
        var result = await RssClassifier.ClassifyAsync(client, item, systemPrompt, ct);
        if (!validSignals.Contains(result.Signal))
        {
            var ex = new InvalidOperationException($"Invalid signal '{result.Signal}' from model");
            ex.Data["HallucinatedSignal"] = result.Signal;
            throw ex;
        }
        return result;
    }

    static VectorClassifier.LlmFallbackDelegate CreateLlmFallback(
        IChatClient chatClient, string systemPrompt, HashSet<string> validSignals)
    {
        return (item, ct) => ClassifyAndValidateAsync(chatClient, item, systemPrompt, validSignals, ct);
    }

    async Task RestoreVectorStateAsync(CancellationToken ct)
    {
        await EnsureCollectionCreatedAsync(collection, ct);

        var labelCounts = await store.GetSignalCountsAsync(ct);
        foreach (var (labelId, _) in labelCounts)
        {
            var items = await store.GetClassifiedItemsByLabelAsync(labelId, limit: 1000, cancellationToken: ct);
            foreach (var c in items)
            {
                if (c.Embedding is null || c.Embedding.Length == 0) continue;
                var emb = EmbeddingSerializer.FromBytes(c.Embedding);
                centroids.AddOrUpdate(c.SignalLabel?.Name ?? "Unknown", emb);
                await collection.UpsertAsync(new SignalFlowVectorEntry
                {
                    Id = c.ContentId.ToString(),
                    Signal = c.SignalLabel?.Name ?? "Unknown",
                    Title = c.RssItem?.Title ?? string.Empty,
                    Summary = c.RssItem?.Body ?? string.Empty,
                    IsNoise = c.IsNoise,
                    ClassifiedAt = c.ClassifiedAt,
                    Embedding = emb
                }, ct);
            }
        }
    }

    async Task PersistResultAsync(
        Content.Content content,
        ReadOnlyMemory<float> embedding,
        ClassificationDecision decision,
        int attemptCount,
        CancellationToken ct)
    {
        var label = await EnsureLabelAsync(decision.Result.Signal, ct);

        var classifiedItem = new ClassifiedItem
        {
            ContentId = content.Id,
            LabelId = label.Id,
            Reasoning = decision.Result.Reasoning,
            IsNoise = decision.Result.IsNoise,
            AttemptCount = attemptCount,
            HallucinatedSignal = decision.Result.HallucinatedSignal,
            Embedding = embedding.IsEmpty ? null : EmbeddingSerializer.ToBytes(embedding),
            ClassificationSource = decision.Source,
            ClassifiedAt = DateTimeOffset.UtcNow
        };
        await store.CreateClassifiedItemAsync(classifiedItem, ct);

        if (!embedding.IsEmpty)
        {
            await collection.UpsertAsync(new SignalFlowVectorEntry
            {
                Id = content.Id.ToString(),
                Signal = decision.Result.Signal,
                Title = content.Title,
                Summary = content.Body ?? string.Empty,
                IsNoise = decision.Result.IsNoise,
                ClassifiedAt = DateTimeOffset.UtcNow,
                Embedding = embedding
            }, ct);

            centroids.AddOrUpdate(decision.Result.Signal, embedding);
        }
    }

    static async Task EnsureCollectionCreatedAsync(
        VectorStoreCollection<string, SignalFlowVectorEntry> collection, CancellationToken ct)
    {
        await collection.EnsureCollectionExistsAsync(ct);
    }

    async Task<Label> EnsureLabelAsync(string name, CancellationToken ct)
    {
        var label = await store.GetLabelByNameAsync(name, ct);
        if (label is not null) return label;

        label = new Label
        {
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
            Color = "#6c757d",
            CreatedAt = DateTimeOffset.UtcNow
        };
        return await store.CreateLabelAsync(label, ct);
    }
}
