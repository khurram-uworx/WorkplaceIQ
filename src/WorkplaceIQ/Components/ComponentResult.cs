using WorkplaceIQ.Containers;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Components;

public sealed record ComponentResult(
    Container? Container,
    IReadOnlyList<Post> Posts,
    bool Created,
    bool Missing,
    string DisplayTitle);
