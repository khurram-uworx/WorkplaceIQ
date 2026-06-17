using WorkplaceIQ.Containers;

namespace WorkplaceIQ.Feeds;

public sealed record FeedComponentResult(
    Container? Container,
    IReadOnlyList<FeedPost> Posts,
    bool Created,
    bool Missing,
    string DisplayTitle);
