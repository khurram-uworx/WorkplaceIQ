using WorkplaceIQ.Content;

namespace WorkplaceIQ.Feeds;

public sealed record FeedComponentResult(
    FeedContent? Container,
    IReadOnlyList<ContentItem> Items,
    bool Created,
    bool Missing,
    string DisplayTitle);
