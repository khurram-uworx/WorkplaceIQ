using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Feeds;

public sealed record FeedComponentResult(
    Container? Container,
    IReadOnlyList<Post> Posts,
    IReadOnlyList<ContentItem> ContentItems,
    bool Created,
    bool Missing,
    string DisplayTitle);
