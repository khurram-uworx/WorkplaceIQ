using WorkplaceIQ.Containers;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Forums;

public sealed record ForumComponentResult(
    Container? Container,
    IReadOnlyList<Post> Posts,
    bool Created,
    bool Missing,
    string DisplayTitle);
