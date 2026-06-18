using WorkplaceIQ.Containers;

namespace WorkplaceIQ.Entities;

public sealed record EntityComponentResult(
    Container? Container,
    IReadOnlyList<BusinessEntity> Entities,
    bool Created,
    bool Missing,
    string DisplayTitle,
    string EntityType);
