using WorkplaceIQ.Content;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Forums;

public sealed record ForumComponentResult(
    Content.Content? Container,
    IReadOnlyList<Post> Posts,
    bool Created,
    bool Missing,
    string DisplayTitle);
