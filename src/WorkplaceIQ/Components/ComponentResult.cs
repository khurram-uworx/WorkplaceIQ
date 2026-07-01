using WorkplaceIQ.Content;

namespace WorkplaceIQ.Components;

public sealed record ComponentResult(
    Container? Container,
    IReadOnlyList<ContentItem> Items,
    bool Created,
    bool Missing,
    string DisplayTitle);
