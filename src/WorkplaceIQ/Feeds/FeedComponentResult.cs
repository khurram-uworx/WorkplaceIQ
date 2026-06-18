using WorkplaceIQ.Content;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Feeds;

public sealed record FeedComponentResult(
    Content.Content? Container,
    IReadOnlyList<Post> Posts,
    IReadOnlyList<Content.Content> ContentItems,
    bool Created,
    bool Missing,
    string DisplayTitle);
