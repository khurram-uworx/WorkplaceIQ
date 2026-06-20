using Microsoft.Extensions.AI;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public static class RssClassifier
{
    public static async Task<ClassificationResult> ClassifyAsync(
        IChatClient client,
        RssItem item,
        string systemPrompt,
        CancellationToken ct = default)
    {
        var userPrompt =
            $"""
            Title: {item.Title}
            Summary: {item.Summary ?? "(no summary)"}
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt),
        };

        var result = await client.GetResponseAsync<ClassificationResult>(messages, cancellationToken: ct);
        return result.Result;
    }
}
