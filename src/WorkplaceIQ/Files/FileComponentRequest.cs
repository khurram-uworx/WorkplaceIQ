namespace WorkplaceIQ.Files;

public sealed record FileComponentRequest(
    string Id,
    string? Title,
    bool AutoProvision);
