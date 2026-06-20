namespace WorkplaceIQ.Web.SignalFlow.Models;

public class PipelineConfig
{
    public List<FeedSource> FeedSources { get; set; } = [];
    public string Goal { get; set; } = string.Empty;
    public string[] Signals { get; set; } = [];
    public string PromptTemplate { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "http://localhost:11434/v1";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public string LlmModel { get; set; } = "llama3.2:1b";
    public string ApiKey { get; set; } = "no-auth";
    public int EmbeddingDimension { get; set; } = 768;
    public string ConfigDir { get; set; } = "configs";
    public int BootstrapThreshold { get; set; } = 20;
    public int MaxConcurrency { get; set; } = 4;
    public double MinAvgSimilarity { get; set; } = 0.86;
    public double MinMargin { get; set; } = 0.10;
    public int TopK { get; set; } = 10;
    public int MinNeighbors { get; set; } = 5;
    public int MinNeighborAgreement { get; set; } = 5;
}

public record FeedSource(string Name, string Url);

public sealed record SignalCountItem(Guid LabelId, string Name, int Count);
