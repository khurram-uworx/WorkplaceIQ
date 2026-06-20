using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public class ConfigLoader
{
    public async Task<PipelineConfig> LoadAsync(string configDir, CancellationToken ct = default)
    {
        var config = new PipelineConfig { ConfigDir = configDir };

        config.FeedSources = await LoadFeedSourcesAsync(configDir, ct);
        config.Goal = await ReadAllTextIfExistsAsync(Path.Combine(configDir, "goal.md"), ct) ?? "";
        config.Signals = await LoadSignalNamesAsync(configDir, ct);
        config.PromptTemplate = await ReadAllTextIfExistsAsync(Path.Combine(configDir, "prompt.md"), ct) ?? "";
        await ApplyEngineSettingsAsync(config, configDir, ct);

        return config;
    }

    static async Task<List<FeedSource>> LoadFeedSourcesAsync(string configDir, CancellationToken ct)
    {
        var path = Path.Combine(configDir, "source.md");
        var content = await ReadAllTextIfExistsAsync(path, ct);
        if (content is null) return [];

        return content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith('-'))
            .Select(l => l.TrimStart('-', ' '))
            .Select(l =>
            {
                var parts = l.Split('|', 2);
                var name = parts[0].Trim();
                var url = parts.Length > 1 ? parts[1].Trim() : name;
                return new FeedSource(name, url);
            })
            .ToList();
    }

    static async Task<string[]> LoadSignalNamesAsync(string configDir, CancellationToken ct)
    {
        var path = Path.Combine(configDir, "signals.md");
        var content = await ReadAllTextIfExistsAsync(path, ct);
        if (content is null) return [];

        return content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith('-'))
            .Select(l => l.TrimStart('-', ' '))
            .ToArray();
    }

    static async Task ApplyEngineSettingsAsync(PipelineConfig config, string configDir, CancellationToken ct)
    {
        var path = Path.Combine(configDir, "engine.md");
        var content = await ReadAllTextIfExistsAsync(path, ct);
        if (content is null) return;

        var settings = content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith('-'))
            .Select(l => l.TrimStart('-', ' '))
            .Select(l =>
            {
                var eq = l.IndexOf('=');
                return eq > 0 ? (Key: l[..eq].Trim(), Value: l[(eq + 1)..].Trim()) : default;
            })
            .Where(t => t.Key is not null);

        foreach (var (key, value) in settings)
        {
            switch (key.ToLowerInvariant())
            {
                case "endpoint":
                case "ollaendpoint" when !string.IsNullOrEmpty(value):
                    config.Endpoint = value; break;
                case "embeddingmodel" when !string.IsNullOrEmpty(value):
                    config.EmbeddingModel = value; break;
                case "llmmodel" when !string.IsNullOrEmpty(value):
                    config.LlmModel = value; break;
                case "apikey":
                    config.ApiKey = value; break;
                case "embeddingdimension" when int.TryParse(value, out var i) && i > 0:
                    config.EmbeddingDimension = i; break;
                case "minavgsimilarity" when double.TryParse(value, out var d):
                    config.MinAvgSimilarity = d; break;
                case "minmargin" when double.TryParse(value, out var d):
                    config.MinMargin = d; break;
                case "bootstrapthreshold" when int.TryParse(value, out var i):
                    config.BootstrapThreshold = i; break;
                case "topk" when int.TryParse(value, out var i):
                    config.TopK = i; break;
                case "minneighbors" when int.TryParse(value, out var i):
                    config.MinNeighbors = i; break;
                case "minneighboragreement" when int.TryParse(value, out var i):
                    config.MinNeighborAgreement = i; break;
            }
        }
    }

    static Task<string?> ReadAllTextIfExistsAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return Task.FromResult<string?>(null);
        return File.ReadAllTextAsync(path, ct);
    }
}
