namespace WorkplaceIQ.Web.SignalFlow.Models;

public sealed record RssItem
{
    public string FeedUrl { get; init; } = string.Empty;
    public string FeedName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Link { get; init; } = string.Empty;
    public DateTimeOffset Published { get; init; }
    public string ContentHash { get; init; } = string.Empty;
}
