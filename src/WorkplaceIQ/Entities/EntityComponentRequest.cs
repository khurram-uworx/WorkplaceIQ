namespace WorkplaceIQ.Entities;

public sealed record EntityComponentRequest(
    string Id,
    string? Title,
    string? Type,
    bool AutoProvision);
