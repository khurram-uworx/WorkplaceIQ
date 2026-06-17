namespace WorkplaceIQ.Feeds;

public sealed record FeedComponentRequest(
    string Id,
    string? Title,
    bool AutoProvision);
