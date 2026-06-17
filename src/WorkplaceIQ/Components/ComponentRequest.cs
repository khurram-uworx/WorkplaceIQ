namespace WorkplaceIQ.Components;

public sealed record ComponentRequest(
    string Id,
    string Title,
    string ContainerType,
    bool AutoProvision,
    string ComponentName);
