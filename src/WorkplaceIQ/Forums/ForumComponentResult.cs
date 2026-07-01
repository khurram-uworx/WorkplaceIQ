using WorkplaceIQ.Content;

namespace WorkplaceIQ.Forums;

public sealed record ForumComponentResult(
    DiscussionContent? Container,
    IReadOnlyList<ContentItem> Items,
    bool Created,
    bool Missing,
    string DisplayTitle);
