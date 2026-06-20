using Microsoft.Extensions.AI;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public class EmbeddingService
{
    public const int DefaultMaxInputChars = 8000;

    readonly IEmbeddingGenerator<string, Embedding<float>> generator;
    readonly int maxInputChars;

    public EmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        int maxInputChars = DefaultMaxInputChars)
    {
        ArgumentNullException.ThrowIfNull(generator);
        if (maxInputChars <= 0) throw new ArgumentOutOfRangeException(nameof(maxInputChars));

        this.generator = generator;
        this.maxInputChars = maxInputChars;
    }

    public ValueTask<ReadOnlyMemory<float>> GenerateAsync(RssItem item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var text = string.IsNullOrWhiteSpace(item.Summary)
            ? item.Title
            : $"{item.Title}\n\n{item.Summary}";
        return GenerateAsync(text, ct);
    }

    public async ValueTask<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ct.ThrowIfCancellationRequested();

        var truncated = text.Length > maxInputChars
            ? text[..maxInputChars]
            : text;

        return await generator.GenerateVectorAsync(truncated, cancellationToken: ct).ConfigureAwait(false);
    }
}
