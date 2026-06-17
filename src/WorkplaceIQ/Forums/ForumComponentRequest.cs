namespace WorkplaceIQ.Forums;

public sealed record ForumComponentRequest(
    string Id,
    string Title,
    bool AutoProvision);
