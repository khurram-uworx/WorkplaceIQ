using WorkplaceIQ.Content;

namespace WorkplaceIQ.Entities;

public sealed record EntityComponentResult(
    Content.Content? Container,
    IReadOnlyList<Content.Content> Entities,
    bool Created,
    bool Missing,
    string DisplayTitle,
    string EntityType);
