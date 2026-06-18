using WorkplaceIQ.Content;

namespace WorkplaceIQ.Files;

public sealed record FileObject(
    ContentItem ContentItem,
    FileRecord FileRecord);
