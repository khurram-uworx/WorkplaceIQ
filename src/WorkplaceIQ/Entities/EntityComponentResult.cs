using WorkplaceIQ.Content;

namespace WorkplaceIQ.Entities;

public sealed record EntityComponentResult(
    GroupContent? Container,
    IReadOnlyList<ContentItem> Entities,
    bool Created,
    bool Missing,
    string DisplayTitle,
    string EntityType);
