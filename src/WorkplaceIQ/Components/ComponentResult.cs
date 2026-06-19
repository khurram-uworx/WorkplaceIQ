using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Components;

public sealed record ComponentResult(
    Content.Content? Container,
    IReadOnlyList<Post> Posts,
    bool Created,
    bool Missing,
    string DisplayTitle);
